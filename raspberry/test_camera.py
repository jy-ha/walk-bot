
resolution_x = 160
resolution_y = 128
blob_threshold = 100
camera = PiCamera()
camera.resolution = (resolution_x, resolution_y)
camera.vflip = True
camera.hflip = True
camera.framerate = 10

raw_capture = PiRGBArray(camera, size=(resolution_x, resolution_y))

for frame in camera.capture_continuous(raw_capture, format="bgr", use_video_port=True):
    raw_capture.truncate(0)
    result = cv2.cvtColor(frame.array, cv2.COLOR_BGR2GRAY)
    # result = cv2.inRange(result, 0, 100)
    # result = cv2.adaptiveThreshold(result, 100, cv2.ADAPTIVE_THRESH_MEAN_C, cv2.THRESH_BINARY, 9, 0)
    _, result = cv2.threshold(result, 100, 255, cv2.THRESH_BINARY) # THRESH_BINARY_INV
    # fisheye 보정법도 찾아보자

    # Labeling
    cnt, labels, stats, centroids = cv2.connectedComponentsWithStats(result)

    # Blob constraint
    for i in range(1, cnt):
        pts =  np.where(labels == i)
        if stats[i][4] < blob_threshold:
            labels[pts] = 0
        else:
            labels[pts] = 255

    img = np.array(labels, dtype=np.uint8)
    cv2.imshow('result', img)

    k = cv2.waitKey(1) & 0xFF
    if k == 27: # ESC
        break


# cap = cv2.VideoCapture(0)
# cap.set(3,320)
# cap.set(4,240)

# print(cap.isOpened())

#while cap.isOpened():
#    ret, frame = cap.read()    
#    if ret:
        #print(frame.shape) 320x240x3
#        result = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
#        result = cv2.inRange(result, 100, 255)

#        cv2.imshow('result', result)

#        k = cv2.waitKey(1) & 0xFF
#        if k == 27: # ESC
#            break


# print(image_array.shape)

# img_result = cv2.resize(image_array, None, fx=0.2, fy=0.2, interpolation = cv2.INTER_AREA)
# plt.imshow(select_color(img_result, np.array([15,80,140]), np.array([55,255,255])), 'Greys_r')
# cv2.imshow('Gray', select_color(img_result, np.array([15,80,140]), np.array([55,255,255])))

# cv2.imshow('test', img_result)
# cv2.waitKey(0)
# cv2.destroyAllWindows()

# BGRtoRGB = img[:, :, ::-1]
# image_array[10,10] 10x10 의 BGR 값 획득
# cv2.rectangle(image_array, (40,40), (80,200), (0,0,0), 5) 이미지, 꼭지점1, 2, 색깔, 두께
