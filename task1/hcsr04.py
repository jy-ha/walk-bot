import time
import RPi.GPIO as GPIO

class HCSR04:

    counter = 0
    distance = 1

    def __init__(self, gpio_trig_, gpio_echo_):
        self.gpio_trig = gpio_trig_
        self.gpio_echo = gpio_echo_

        GPIO.setmode(GPIO.BOARD)
        GPIO.setup(self.gpio_trig ,GPIO.OUT)
        GPIO.setup(self.gpio_echo ,GPIO.IN)

        GPIO.add_event_detect(self.gpio_echo, edge=GPIO.BOTH, callback=self.callback_echo)

    def pulse(self):
        GPIO.output(self.gpio_trig, True)
        time.sleep(50.0 / 1000000.0)
        GPIO.output(self.gpio_trig, False)

    def callback_echo(self, pin):
        if GPIO.input(self.gpio_echo) == GPIO.HIGH:
            self.counter = time.time()
        else:
            travel_time = time.time() - self.counter
            self.distance = travel_time * 340 / 2


