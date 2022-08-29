#!/bin/bash

# Exit on first error, echo to stdout
set -ex

reboot_needed=0

# Configure DWC in modules and boot config
if grep -Fxq "dtoverlay=dwc2" /boot/config.txt
then
    echo "dwc2 already in /boot/config.txt"
else
    echo "dtoverlay=dwc2" >> /boot/config.txt
    reboot_needed=1
fi

if grep -Fxq "dwc2" /etc/modules    
then
    echo "dwc2 already in /modules"
else
    echo "dwc2" >> /etc/modules
    reboot_needed=1
fi

if [ "$reboot_needed" = "1" ]; then
    echo "Pausing for a few seconds, then rebooting..."
    sleep 5
    reboot
    exit 0
fi
 
# Install usb-hid gadget
echo "Installing USB HID gadget..."
./usb-gadget/init-usb-gadget.sh
./usb-gadget/install-usb-gadget.sh

set +x

echo "----------------------------------------------------------"
echo "   COMPLETE! Your Pi is now a XaC-Assist capable host!"
echo "----------------------------------------------------------"