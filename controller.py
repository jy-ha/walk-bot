PORT = 8000

from http.client import HTTPConnection
import json
from time import sleep
# import numpy as np
import datetime
from sys import argv
import RPi.GPIO as GPIO

from motor import Navigator

############## GPIO ##############
MOT_LF = 16
MOT_LB = 18
MOT_RF = 13
MOT_RB = 11
##################################

print(argv)

Time = lambda: datetime.datetime.now().time()
HALF=.5
MOTOR_SPEEDS = {
        "q": (-1, 1), "w": (1, 1), "e": (1, -1),
    "a": (-1, 1), "s": (0, 0), "d": (1, -1),
    "z": (-HALF, -1), "x": (-1, -1), "c": (1, HALF),
}

def main():
    GPIO.cleanup()
    nav = Navigator(MOT_LF, MOT_LB, MOT_RF, MOT_RB)

    while True:
        conn = HTTPConnection(f"{argv[1] if len(argv) > 1 else  'localhost'}:{PORT}")

        try:
            conn.request("GET", "/")
        except ConnectionRefusedError as error:
            print(error)
            sleep(1)
            continue

        print('Connected')
        res = conn.getresponse()
        while True:
            chunk = res.readline()
            if (chunk == b'\n'): continue
            if (not chunk): break

            chunk = chunk[:-1].decode()
            data = json.loads(chunk)
            print(Time, data)
            action = data['action']
            # print('action', action)
            try:
                print(MOTOR_SPEEDS[action])
                nav.set_speed(MOTOR_SPEEDS[action][0], MOTOR_SPEEDS[action][1])
            except KeyError as error:
                print(error)


main()
