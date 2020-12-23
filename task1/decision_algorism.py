
class DecisionAlgorism:

    action_order = 'foward'
    street_state = 'foward'
    #threshold_collision = 28
    #threshold_turn = 43
    #threshold_available = 50
    #margin = 10
    threshold_collision = 42
    threshold_turn = 65
    threshold_available = 75
    margin = 15

    counter = 0
    turn_start_left = False
    turn_start_right = False
    turn_timer_foward = 0.6
    turn_timer_turn = 1.1

    stop_start = False
    stop_timer = 5
    stop_cooldown = 8

    finish_start = False
    finish_timer = 1.5

    def __init__(self, time_now):
        self.time_last = time_now

    def decide(self, distances, order_override, time_now):
        time_delta = time_now - self.time_last
        self.time_last = time_now
        self.counter += time_delta
        next_action = 'stop'

        if self.turn_start_left == True:
            if self.counter < self.turn_timer_turn:
                override = 'turn_left'
            else:
                self.turn_start_left = False
                override = 'foward'

        elif self.turn_start_right == True:
            if self.counter < self.turn_timer_turn:
                override = 'turn_right'
            else:
                self.turn_start_right = False
                override = 'foward'
            
        elif self.stop_start == True:
            if self.counter < self.stop_timer:
                override = 'stop'
            elif self.counter < self.stop_cooldown:
                override = 'foward'
            else:
                self.stop_start = False
                override = 'foward'

        elif self.finish_start == True:
            if self.counter < self.finish_timer:
                override = 'foward'
            else:
                override = 'finish'

        else:
            override = order_override

        #############################################################
        if override == 'foward':
            if self.action_order == 'foward' :
                if distances[2] < self.threshold_collision: # something wrong
                    next_action = 'backward'
                elif distances[2] < self.threshold_turn: # 코너링
                    if (distances[0] + distances[1] + self.margin) < (distances[3] + distances[4]): # 좌회전모드
                        self.action_order = 'turn_right'
                        next_action = 'turn_left'
                    elif (distances[0] + distances[1]) > (distances[3] + distances[4] + self.margin): # 우회전모드
                        self.action_order = 'turn_left'
                        next_action = 'turn_right'
                    else: # 애매할땐 이전 상태로 추측
                        if self.street_state == 'turn_left':
                            self.action_order = 'turn_left'
                            next_action = 'turn_left'
                        elif self.street_state == 'turn_right':
                            self.action_order = 'turn_right'
                            next_action = 'turn_right'
                        else:
                            next_action = 'backward'

                elif distances[0] < self.threshold_collision: # 왼쪽 충돌 주의
                    next_action = 'turn_right'
                elif distances[4] < self.threshold_collision: # 오른쪽 충돌 주의
                    next_action = 'turn_left'
                else: # 직진하며 길 상태 기록
                    if (distances[0] + distances[1] + self.margin) < (distances[3] + distances[4]): # 우회전길 있음
                        self.street_state = 'turn_right'
                        print('avail_r')
                    elif (distances[0] + distances[1]) > (distances[3] + distances[4] + self.margin): # 좌회전길 있음
                        self.street_state = 'turn_left'
                        print('avail_l')
                    next_action = 'foward'

            elif self.action_order == 'turn_left' :
                if distances[2] > self.threshold_available: # 직진모드로
                    self.action_order = 'foward'
                    next_action = 'foward'
                else:
                    next_action = 'turn_left'

            elif self.action_order == 'turn_right' :
                if distances[2] > self.threshold_available: # 직진모드로
                    self.action_order = 'foward'
                    next_action = 'foward'
                else:
                    next_action = 'turn_right'

        #############################################################
        elif override == 'turn_left':
            if self.turn_start_left == True:
                if self.counter < self.turn_timer_foward:
                    next_action = 'foward'
                else:
                    next_action = 'turn_left'
            else:
                self.turn_start_left = True
                self.counter = 0
                next_action = 'foward'


        #############################################################
        elif override == 'turn_right':
            if self.turn_start_right == True:
                if self.counter < self.turn_timer_foward:
                    next_action = 'foward'
                else:
                    next_action = 'turn_right'
            else:
                self.turn_start_right = True
                self.counter = 0
                next_action = 'foward'

        #############################################################
        elif override == 'stop':
            if self.stop_start == True:
                next_action = 'stop'
            else:
                self.stop_start = True
                self.counter = 0
                next_action = 'stop'

        #############################################################
        elif override == 'wall':
            next_action = 'stop'

        #############################################################
        elif override == 'finish':
            if self.finish_start == True:
                next_action = 'finish'
            else:
                self.finish_start = True
                self.counter = 0
                next_action = 'foward'

        return next_action




