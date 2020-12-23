import os
import cv2
import math
import time
import numpy as np
from picamera import PiCamera
from picamera.array import PiRGBArray
from ar_markers import detect_markers
#import matplotlib
#matplotlib.use('tkagg')
#import matplotlib.pyplot as plt
# print(cv2.__version__)

class CameraSensor:
    #pt_center = (79, 127)
    pt_center = (119, 191)
    #pt_refs = [(0, 85), (0, 43), (0, 0), (79, 0), (159, 0), (159, 43), (159, 85)]
    #pt_refs = [(0, 85), (0, 43), (79, 0), (159, 43), (159, 85)]
    pt_refs = [(0, 128), (0, 65), (119, 0), (239, 65), (239, 128)]
    distances = []

    def __init__(self, threshold=100, res_x=240, res_y=192, blob_threshold_=100, framerate=10):
        self.distances = [0 for i in range(len(self.pt_refs))]
        self.resolution_x = res_x
        self.resolution_y = res_y
        self.blob_threshold = blob_threshold_
        self.threshold_gray = threshold
        contrast = 0
        self.contrast_alpha = 131*(contrast + 127)/(127*(131-contrast))
        self.contrast_gamma = 127*(1-self.contrast_alpha)

        self.camera = PiCamera()
        self.camera.resolution = (res_x, res_y)
        self.camera.vflip = True
        self.camera.hflip = True
        self.camera.framerate = framerate
        self.raw_capture = PiRGBArray(self.camera, size=(res_x, res_y))

        self.map1 = np.load(file="map1.npy")
        self.map2 = np.load(file="map2.npy")
        


    def process_image(self, frame):
        self.raw_capture.truncate(0)
        self.img_gray = cv2.cvtColor(frame.array, cv2.COLOR_BGR2GRAY)
        self.img_gray = cv2.addWeighted(self.img_gray, self.contrast_alpha, self.img_gray, 0, self.contrast_gamma)
        _, self.img_binary = cv2.threshold(self.img_gray, self.threshold_gray, 255, cv2.THRESH_BINARY) # THRESH_BINARY_INV
        #self.img_gray = cv2.remap(self.img_gray, self.map1, self.map2, interpolation=cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT) # fisheye 보정
        
        # Labeling
        cnt, labels, stats, centroids = cv2.connectedComponentsWithStats(self.img_binary)

        # Blob constraint
        for i in range(1, cnt):
            pts =  np.where(labels == i)
            if stats[i][4] < self.blob_threshold:
                labels[pts] = 0
            else:
                labels[pts] = 255
        self.img_binary = np.array(labels, dtype=np.uint8)


    def detect_distance(self):
        for i, pt in enumerate(self.pt_refs):
            self.distances[i] = self.get_distance_to_wall(self.img_binary, self.pt_center, pt)
        
        #for pt in self.pt_refs:
        #    self.img_gray = cv2.line(self.img_gray, self.pt_center, pt, (255,0,0), 1)
        
        return self.distances


    def detect_marker(self):
        markers = detect_markers(self.img_gray)
        for marker in markers:
            print(marker.id)
            return marker.id
        return 0


    def detect_stopsign(self):
        objs_cascade = cv2.CascadeClassifier('./cascade.xml')
        objs = objs_cascade.detectMultiScale(self.img_gray, 
                                            cv2.COLOR_BGR2GRAY
                                            )
        for (x,y,w,h) in objs:
            #cv2.rectangle(self.img_gray, (x,y), (x+w, y+h), (255, 0, 0), 1)
            print('stop')
            return True
        return False


    def get_distance_to_wall(self, img, pt_start, pt_end):
        distance = 0
        while pt_start[1] - distance > pt_end[1]:
            check_y = pt_start[1] - distance
            check_x = round(pt_start[0] + ((pt_end[0] - pt_start[0])/(pt_start[1] - pt_end[1]) * (pt_start[1] - check_y)))
            #print(check_y, check_x)
            buff = img[check_y, check_x]
            if buff != 0:
                break
            distance += 1
        return distance
