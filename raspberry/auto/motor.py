import time
from RPi import GPIO

class Navigator:
    duty_cycle = 100
    gpio_left_foward = 0
    gpio_left_back = 0
    gpio_right_foward = 0
    gpio_right_back = 0
    pwm_left_foward = 0
    pwm_left_back = 0
    pwm_right_foward = 0
    pwm_right_back = 0

    def __init__(self, 
                gpio_left_foward_, 
                gpio_left_back_, 
                gpio_right_foward_, 
                gpio_right_back_):

        self.gpio_left_foward = gpio_left_foward_
        self.gpio_left_back = gpio_left_back_
        self.gpio_right_foward = gpio_right_foward_
        self.gpio_right_back = gpio_right_back_

        GPIO.setmode(GPIO.BOARD)
        GPIO.setup(self.gpio_left_foward ,GPIO.OUT)
        GPIO.setup(self.gpio_left_back ,GPIO.OUT)
        GPIO.setup(self.gpio_right_foward ,GPIO.OUT)
        GPIO.setup(self.gpio_right_back ,GPIO.OUT) # initial=GPIO.HIGH ?
        self.pwm_left_foward = GPIO.PWM(self.gpio_left_foward, self.duty_cycle)
        self.pwm_left_back = GPIO.PWM(self.gpio_left_back, self.duty_cycle)
        self.pwm_right_foward = GPIO.PWM(self.gpio_right_foward, self.duty_cycle)
        self.pwm_right_back = GPIO.PWM(self.gpio_right_back, self.duty_cycle)
        self.pwm_left_foward.start(0)
        self.pwm_left_back.start(0)
        self.pwm_right_foward.start(0)
        self.pwm_right_back.start(0)

    def set_speed(self, left, right): # -1 ~ 1
        speed_left = self.duty_cycle * left
        speed_right = self.duty_cycle * right

        if speed_left > 0:
            self.pwm_left_foward.ChangeDutyCycle(speed_left)
            self.pwm_left_back.ChangeDutyCycle(0)
        else:
            self.pwm_left_back.ChangeDutyCycle(-speed_left)
            self.pwm_left_foward.ChangeDutyCycle(0)

        if speed_right > 0:
            self.pwm_right_foward.ChangeDutyCycle(speed_right)
            self.pwm_right_back.ChangeDutyCycle(0)
        else:
            self.pwm_right_back.ChangeDutyCycle(-speed_right)
            self.pwm_right_foward.ChangeDutyCycle(0)

        
    def stop(self):
        self.pwm_left_foward.ChangeDutyCycle(0)
        self.pwm_left_back.ChangeDutyCycle(0)
        self.pwm_right_foward.ChangeDutyCycle(0)
        self.pwm_right_back.ChangeDutyCycle(0)


#MOT_LF = 16
#MOT_LB = 18
#MOT_RF = 13
#MOT_RB = 11
#nav = Navigator(MOT_LF, MOT_LB, MOT_RF, MOT_RB)
#nav.set_speed(1, 1)
#time.sleep(2)
#GPIO.cleanup()
