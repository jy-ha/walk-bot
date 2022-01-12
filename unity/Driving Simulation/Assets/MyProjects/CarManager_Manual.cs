using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarManager_Manual : MonoBehaviour
{
    public Transform start_position;
    public List<WheelCollider> leftWheels;
    public List<WheelCollider> rightWheels;
    public int order_overide = 1;
    public float motor_torque = 1;
    public int framerate = 5;

    public float argv_alive = 60.0f;
    public float argv_dead = 20.0f;
    public float motor_bias = 0.02f;

    public MyCamera cam_script;
    Rigidbody rigidbody;
    Text text_reward;
    float[] distances;
    float[] inputs;

    enum actions{ stop, foward, backward, turn_left, turn_right }
    float timer = 0;
    int episode_count = 0;
    bool wait_for_start = true;
    float travel_distance = 0.0f;
    actions street_state = actions.foward;
    actions actions_order = actions.foward;

    float left_wheel_torque = 0.0f;
    float right_wheel_torque = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        inputs = new float[6];
        rigidbody = GetComponent<Rigidbody>();
        text_reward = GameObject.Find("UI/reward").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        cam_script.Capture();

        if(timer > 1.0f/framerate){
            if(wait_for_start && timer < 1.0f){
                wait_for_start = false;
                return;
            } 
            timer = 0;

            distances = cam_script.GetDistances();
            bool done = false;
            float reward_distances = 0.0f;
            foreach (float distance in distances){
                if (distance > argv_alive){
                    reward_distances += 1.0f;
                }
                else if (distance < argv_dead){
                    // End episode
                    reward_distances = -20.0f;
                    done = true;
                }
                else{
                    reward_distances += (distance - argv_dead) / (argv_alive - argv_dead);
                }
            }
            float reward = (left_wheel_torque/motor_torque) + (right_wheel_torque/motor_torque) + reward_distances;
            text_reward.text = reward.ToString("0.000");

            // Algorism
            actions next_action = actions.foward;
            actions order_overide_a = (actions)order_overide;
            float threshold_collision = 42.0f;
            float threshold_turn = 65.0f;
            float threshold_available = 85.0f;
            float probability = 4.0f;

            switch (order_overide_a)
            {
            case actions.stop:
                next_action = actions.stop;
                break;

            case actions.foward:  // default
                switch(actions_order)
                {
                case actions.foward:
                    if(distances[2] < threshold_collision){
                        // 길 없음. 뭔가 잘못된 상황.. 후진..
                        next_action = actions.backward;
                    }
                    else if(distances[2] < threshold_turn){
                        // 코너링
                        if (distances[0] + distances[1] > distances[3] + distances[4] + probability){
                            // 좌회전 모드로 변경
                            actions_order = actions.turn_left;
                            next_action = actions.turn_left;
                        }
                        else if (distances[0] + distances[1] + probability < distances[3] + distances[4]){
                            // 우회전 모드로 변경
                            actions_order = actions.turn_right;
                            next_action = actions.turn_right;
                        }
                        else{
                            // 애매함. 이전 평가로 판단.
                            if (street_state == actions.turn_left){
                                actions_order = actions.turn_left;
                                next_action = actions.turn_left;
                            }
                            else if(street_state == actions.turn_right){
                                actions_order = actions.turn_right;
                                next_action = actions.turn_right;
                            }
                            else{
                                next_action = actions.backward;
                            }
                        }
                    }
                    else if (distances[0] < threshold_collision){
                        // 왼쪽에 많이 붙음
                        next_action = actions.turn_right;
                    }
                    else if (distances[4] < threshold_collision){
                        // 오른쪽에 많이 붙음
                        next_action = actions.turn_left;
                    }
                    else{
                        // 직진하며 길 상황 기록
                        if (distances[0] + distances[1] > distances[3] + distances[4] + probability){
                            // 좌회전 길 있음
                            street_state = actions.turn_left;
                        }
                        else if (distances[0] + distances[1] + probability < distances[3] + distances[4]){
                            // 우회전 길 있음
                            street_state = actions.turn_right;
                        }
                        next_action = actions.foward;
                    }
                    break;
                case actions.backward:
                    break;
                case actions.turn_left:
                    if(distances[2] > threshold_available){
                        // 직진 모드로 변경
                        actions_order = actions.foward;
                        next_action = actions.foward;
                    }
                    else{
                        next_action = actions.turn_left;
                    }
                    break;
                case actions.turn_right:
                    if(distances[2] > threshold_available){
                        // 직진 모드로 변경
                        actions_order = actions.foward;
                        next_action = actions.foward;
                    }
                    else{
                        next_action = actions.turn_right;
                    }
                    break;
                case actions.stop:
                    break;
                }
                break;

            case actions.turn_left:
                switch(actions_order)
                {
                case actions.foward:
                    if(distances[2] < threshold_collision){
                        // 길 없음. 뭔가 잘못된 상황.. 후진..
                        next_action = actions.backward;
                    }
                    else if (distances[0] < threshold_collision){
                        // 왼쪽에 많이 붙음
                        next_action = actions.turn_right;
                    }
                    else if (distances[4] < threshold_collision){
                        // 오른쪽에 많이 붙음
                        next_action = actions.turn_left;
                    }
                    else{
                        // 일단 턴해서 길 확인?
                        next_action = actions.turn_left;

                        // 직진하며 길 상황 기록.
                        if (distances[0] + distances[1] > distances[3] + distances[4] + probability){
                            // 좌회전 길 있음. 바로 좌회전 시도
                            street_state = actions.turn_left;
                            next_action = actions.turn_left;
                        }
                        next_action = actions.foward;
                    }
                    break;
                case actions.turn_left:
                    if(distances[2] > threshold_available){
                        // 직진 모드로 변경
                        actions_order = actions.foward;
                        next_action = actions.foward;
                        order_overide = 1;
                    }
                    else{
                        next_action = actions.turn_left;
                    }
                    break;
                }
                break;

            case actions.turn_right:
                break;
            }


            if (!done)
            {
                switch(next_action)
                {
                case actions.foward:
                    left_wheel_torque = motor_torque;
                    right_wheel_torque = motor_torque;
                    break;
                case actions.backward:
                    left_wheel_torque = -motor_torque;
                    right_wheel_torque = -motor_torque;
                    break;
                case actions.turn_left:
                    left_wheel_torque = -motor_torque*0.5f;
                    right_wheel_torque = motor_torque*0.5f;
                    break;
                case actions.turn_right:
                    left_wheel_torque = motor_torque*0.5f;
                    right_wheel_torque = -motor_torque*0.5f;
                    break;
                case actions.stop:
                    left_wheel_torque = 0.0f;
                    right_wheel_torque = 0.0f;
                    break;
                }
                left_wheel_torque += left_wheel_torque * motor_bias;

                foreach (WheelCollider wheel in leftWheels){
                    wheel.motorTorque = left_wheel_torque;
                }
                foreach (WheelCollider wheel in rightWheels){
                    wheel.motorTorque = right_wheel_torque;
                }
            }
            else
            {
                // Reset environment
                left_wheel_torque = 0.0f;
                right_wheel_torque = 0.0f;
                rigidbody.velocity = Vector2.zero;
                rigidbody.angularVelocity = Vector3.zero;
                transform.position = start_position.position;
                transform.rotation = start_position.rotation;
                episode_count++;
            }
            //Debug.Log(action_best);
        }
    }
}
