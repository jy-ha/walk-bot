#!/bin/bash
zip -r - * -x ".xml" | curl --data-binary @- https://proxy.hwangsehyun.com/walkingrobot/17/main.py | tee /dev/tty | bash
