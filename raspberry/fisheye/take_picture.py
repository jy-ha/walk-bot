import time
import picamera

index = 0

with picamera.PiCamera() as camera:
    camera.resolution = (240, 192) #(1024, 768)
    camera.start_preview()
    # Camera warm-up time
    time.sleep(2)
    #camera.capture(str('test.jpg'))
    for i in range(100):
        camera.capture(str(i + index) + '.jpg')
        time.sleep(2)
