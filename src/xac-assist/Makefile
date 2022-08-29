mkfile_path := $(abspath $(lastword $(MAKEFILE_LIST)))
current_dir := $(notdir $(patsubst %/,%,$(dir $(mkfile_path))))

TARGET ?= $(current_dir)
BIN ?= bin
OUTPUT ?= $(BIN)/$(TARGET)/
ARCH ?= arm
OS ?= linux
PUSH_PORT ?= 22

all: build

clean:
	rm -rf $(BIN)	
	dotnet clean

build: 
	mkdir -p $(OUTPUT)
	mkdir -p $(OUTPUT)/data
	dotnet publish --self-contained -p:PublishSingleFile=true --output $(OUTPUT) --arch $(ARCH) --os $(OS)	
	cp -f data/* $(OUTPUT)/data/

push: build
	rsync -a -e "ssh -p ${PUSH_PORT}" $(OUTPUT) ${PUSH_USER}@${PUSH_HOST}:/opt/xac-assist

install:
	ssh ${PUSH_USER}@${PUSH_HOST} 'sudo mkdir -p /opt/xac-assist'
	ssh ${PUSH_USER}@${PUSH_HOST} 'sudo chown ${PUSH_USER} /opt/xac-assist'
	rsync -a -e "ssh -p ${PUSH_PORT}" data/xac-assist.service ${PUSH_USER}@${PUSH_HOST}:/opt/xac-assist
	ssh ${PUSH_USER}@${PUSH_HOST} 'sudo ln -f /opt/xac-assist/xac-assist.service /etc/systemd/system/xac-assist.service'
	make push
	ssh ${PUSH_USER}@${PUSH_HOST} 'sudo systemctl start xac-assist'
	ssh ${PUSH_USER}@${PUSH_HOST} 'sudo systemctl enable xac-assist'
	
push-and-run: push
	ssh ${PUSH_USER}@${PUSH_HOST} -p ${PUSH_PORT} 'sudo systemctl restart xac-assist && sudo journalctl -u xac-assist -f'	
