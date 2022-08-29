#!/bin/bash

# Exit on first error, echo to stdout, undefined == error
set -exu

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
source "${SCRIPT_DIR}/usb-gadget-helpers.sh"

print_help() {
  cat << EOF
Usage: ${0##*/} [-h]
Init USB gadget.
  -h Display this help and exit.
EOF
}

# Parse command-line arguments.
while getopts "h" opt; do
  case "${opt}" in
    h)
      print_help
      exit
      ;;
    *)
      print_help >&2
      exit 1
  esac
done

modprobe libcomposite

# Adapted from https://github.com/girst/hardpass-sendHID/blob/master/README.md

cd "${USB_GADGET_PATH}"
mkdir -p "${USB_DEVICE_DIR}"
cd "${USB_DEVICE_DIR}"

echo 0x1d6b > idVendor  # Linux Foundation
echo 0x0104 > idProduct # Multifunction Composite Gadget
echo 0x0100 > bcdDevice # v1.0.0
echo 0x0200 > bcdUSB    # USB2

mkdir -p "$USB_STRINGS_DIR"
echo "xac001" > "${USB_STRINGS_DIR}/serialnumber"
echo "xac-assist" > "${USB_STRINGS_DIR}/manufacturer"
echo "xac-assist-otg-controller" > "${USB_STRINGS_DIR}/product"

### We wire up an OTG gadget for a generic gamepad with 16 buttons (2 bytes) and 9 axis (9 bytes)

mkdir -p "$USB_JOYSTICK_FUNCTIONS_DIR"
echo 0 > "${USB_JOYSTICK_FUNCTIONS_DIR}/protocol" 
echo 0 > "${USB_JOYSTICK_FUNCTIONS_DIR}/subclass" 
echo 11 > "${USB_JOYSTICK_FUNCTIONS_DIR}/report_length"

# Write the report descriptor
D=$(mktemp)
{
  echo -ne \\x05\\x01 # USAGE_PAGE (Generic Desktop)
  echo -ne \\x09\\x05 # USAGE (Game Pad)
  echo -ne \\xa1\\x01 # COLLECTION (Application)
  echo -ne \\xa1\\x00 #    COLLECTION (Physical)
  ##### \\x85\\x01  #        REPORT_ID (1)??
  echo -ne \\x05\\x09 #        USAGE_PAGE (Button)
  echo -ne \\x19\\x01 #        USAGE_MINIMUM (Button 1)
  echo -ne \\x29\\x0B #        USAGE_MAXIMUM (Button 11)
  echo -ne \\x15\\x00 #        LOGICAL_MINIMUM (0)
  echo -ne \\x25\\x01 #        LOGICAL_MAXIMUM (1)
  echo -ne \\x95\\x10 #        REPORT_COUNT (16)
  echo -ne \\x75\\x01 #        REPORT_SIZE (1)
  echo -ne \\x81\\x02 #        INPUT (Data,Var,Abs)
  echo -ne \\x05\\x01 #        USAGE_PAGE (Generic Desktop)
  echo -ne \\x09\\x30 #        USAGE (X)
  echo -ne \\x09\\x31 #        USAGE (Y)
  echo -ne \\x09\\x32 #        USAGE (Z)
  echo -ne \\x09\\x33 #        USAGE (Rx)
  echo -ne \\x09\\x34 #        USAGE (X)
  echo -ne \\x09\\x35 #        USAGE (Y)
  echo -ne \\x09\\x36 #        USAGE (Z)
  echo -ne \\x09\\x37 #        USAGE (Rx)
  echo -ne \\x09\\x38 #        USAGE (Rx)
  echo -ne \\x15\\x81 #        LOGICAL_MINIMUM (-127)
  echo -ne \\x25\\x7f #        LOGICAL_MAXIMUM (127)
  echo -ne \\x75\\x08 #        REPORT_SIZE (8)
  echo -ne \\x95\\x09 #        REPORT_COUNT (4)
  echo -ne \\x81\\x02 #        INPUT (Data,Var,Abs)
  echo -ne \\xc0      #    END COLLECTION
  echo -ne \\xc0      # END COLLECTION
} >> "$D"
cp "$D" "${USB_JOYSTICK_FUNCTIONS_DIR}/report_desc"

hexdump $D


mkdir -p "${USB_CONFIG_DIR}"
echo 30 > "${USB_CONFIG_DIR}/MaxPower"

CONFIGS_STRINGS_DIR="${USB_CONFIG_DIR}/${USB_STRINGS_DIR}"
mkdir -p "${CONFIGS_STRINGS_DIR}"
echo "Config ${USB_CONFIG_INDEX}: ECM network" > "${CONFIGS_STRINGS_DIR}/configuration"

ln -s "${USB_JOYSTICK_FUNCTIONS_DIR}" "${USB_CONFIG_DIR}/"

usb_gadget_activate