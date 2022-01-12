import os
import cv2
import time
#import numpy as np
from picamera import PiCamera
from picamera.array import PiRGBArray
import RPi.GPIO as GPIO

from motor import Navigator
from camera_sensor import CameraSensor
from decision_algorithm import DecisionAlgorithm
from hcsr04 import HCSR04


############## GPIO ##############
MOT_LF = 16
MOT_LB = 18
MOT_RF = 13
MOT_RB = 11
HCSR04_TRIG = 8
HCSR04_ECHO = 10
##################################

CAMERA_TEST = False
threshold_brightness = 120
motor_torque = 1.0

GPIO.cleanup()
nav = Navigator(MOT_LF, MOT_LB, MOT_RF, MOT_RB)
hc = HCSR04(HCSR04_TRIG, HCSR04_ECHO)

cs = CameraSensor(threshold=threshold_brightness)
cs.camera.start_preview()
da = DecisionAlgorism(time.time())

time.sleep(2)
for frame in cs.camera.capture_continuous(cs.raw_capture, format="bgr", use_video_port=True):

    order_override = 'foward'
    cs.process_image(frame)
    distances = cs.detect_distance()

    # hc-sr04
    hc.pulse()

    # stop sign detect
    if cs.detect_stopsign() == True:
        order_override = 'stop'

    # ar marker detect
    marker = cs.detect_marker()
    if (marker > 100 and marker < 200) or marker == 1480:
        order_override = 'turn_left'
    elif marker > 900 and marker < 960:
        order_override = 'turn_right'
    elif marker == 841 or marker == 777 or marker == 2537:
        order_override = 'finish'
    elif hc.distance < 0.15:
        order_override = 'wall'


    # decision algorism
    next_action = da.decide(distances, order_override, time.time())
    #next_action = 'foward'

    # control motor
    if not CAMERA_TEST:
        if next_action == 'foward' :
            nav.set_speed(motor_torque, motor_torque)
        elif next_action == 'turn_left' :
            nav.set_speed(-motor_torque, motor_torque)
        elif next_action == 'turn_right' :
            nav.set_speed(motor_torque, -motor_torque)
        elif next_action == 'stop' :
            nav.set_speed(0, 0)
        elif next_action == 'backward' :
            nav.set_speed(-motor_torque, -motor_torque)
        elif next_action == 'finish' :
            break
    #else:
    #    cv2.imshow('result', cs.img_binary)
    #    cv2.imshow('result', cs.img_gray)
    #    k = cv2.waitKey(1) & 0xFF
    #    if k == 27: # ESC
    #        break

    print(distances, next_action)
