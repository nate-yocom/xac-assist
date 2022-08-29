#!/bin/bash

# Exit on first error, echo to stdout, undefined == error
set -exu

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"

# Make sure not already there (else start will fail)
${SCRIPT_DIR}/remove-usb-gadget.sh

mkdir -p /opt/usb-gadget/
cp ${SCRIPT_DIR}/* /opt/usb-gadget/

ln -f /opt/usb-gadget/usb-gadget.systemd.service /etc/systemd/system/usb-gadget.service
systemctl start usb-gadget
systemctl enable usb-gadget