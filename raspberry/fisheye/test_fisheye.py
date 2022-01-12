import cv2
import numpy as np
import sys

# You should replace these 3 lines with the output in calibration step
DIM=(240, 192)
K=np.array([[108.21019251022773, 0.0, 114.75559824098661], [0.0, 108.42864498939103, 103.73958753827495], [0.0, 0.0, 1.0]])
D=np.array([[-0.05693652765834971], [0.03494237385147291], [-0.07323789017967597], [0.03934016106055388]])
def undistort(img_path):
    img = cv2.imread(img_path)
    h,w = img.shape[:2]
    map1, map2 = cv2.fisheye.initUndistortRectifyMap(K, D, np.eye(3), K, DIM, cv2.CV_16SC2)
    undistorted_img = cv2.remap(img, map1, map2, interpolation=cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
    cv2.imwrite("undistorted.jpg", undistorted_img)
    
    np.save('map1.npy',map1)
    np.save('map2.npy',map2)
    
    
if __name__ == '__main__':
    for p in sys.argv[1:]:
        undistort(p)
