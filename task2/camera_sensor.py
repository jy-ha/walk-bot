import os
import cv2
import math
import time
import numpy as np
from picamera import PiCamera
from picamera.array import PiRGBArray

class CameraSensor:

    def __init__(self, margin=10, res_x=160, res_y=128, blob_threshold_=100, framerate=10):
        self.lower_red = (150-margin, 10, 10)
        self.upper_red = (150+margin, 255, 255)
        self.resolution_x = res_x
        self.resolution_y = res_y
        self.blob_threshold = blob_threshold_

        self.camera = PiCamera()
        self.camera.resolution = (res_x, res_y)
        self.camera.vflip = True
        self.camera.hflip = True
        self.camera.framerate = framerate
        self.raw_capture = PiRGBArray(self.camera, size=(res_x, res_y))
        

    def process_image(self, frame):
        self.raw_capture.truncate(0)

        self.img_bgr = frame.array
        self.img_hsv = cv2.cvtColor(frame.array, cv2.COLOR_BGR2HSV)
        self.img_mask = cv2.inRange(self.img_hsv, self.lower_red, self.upper_red)

        M = cv2.moments(self.img_mask)
        if M["m00"] != 0:
            cX = int(M["m10"] / M["m00"])
            cY = int(M["m01"] / M["m00"])
            cv2.circle(self.img_bgr, (cX, cY), 2, (255, 0, 0), 2)

            return ((cX - self.resolution_x/2) / self.resolution_x, (cY - self.resolution_y/2) / self.resolution_y)
        else:
            return (-1, 1)




