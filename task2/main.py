import os
import cv2
import time
#import numpy as np
from picamera import PiCamera
from picamera.array import PiRGBArray
import RPi.GPIO as GPIO

from motor import Navigator
from camera_sensor import CameraSensor


############## GPIO ##############
MOT_LF = 16
MOT_LB = 18
MOT_RF = 13
MOT_RB = 11
##################################

CAMERA_TEST = False
motor_torque = 1.0

GPIO.cleanup()
nav = Navigator(MOT_LF, MOT_LB, MOT_RF, MOT_RB)

cs = CameraSensor(margin=100)
cs.camera.start_preview()

time.sleep(2)
for frame in cs.camera.capture_continuous(cs.raw_capture, format="bgr", use_video_port=True):

    # decision algorism
    target = cs.process_image(frame)
    next_action = 'stop'
    if target[1] < 0:
        if target[0] < -0.1:
            next_action = 'turn_left'
        elif target[0] > 0.1:
            next_action = 'turn_right'
        else:
            next_action = 'foward'
    print(target)

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
    else:
        cv2.imshow('result', cs.img_bgr)
        #cv2.imshow('result', cs.img_mask)
        k = cv2.waitKey(1) & 0xFF
        if k == 27: # ESC
            break
