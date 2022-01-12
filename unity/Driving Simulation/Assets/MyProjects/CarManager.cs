using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarManager : MonoBehaviour
{
    public Transform start_position;
    public List<WheelCollider> leftWheels;
    public List<WheelCollider> rightWheels;
    public float motor_torque = 1;
    public int framerate = 5;
    public int episodes = 10;

    public float argv_alive = 60.0f;
    public float argv_dead = 20.0f;

    public MyCamera cam_script;
    public DeepQ q_script;
    Rigidbody rigidbody;
    Text text_reward;
    float[] distances;
    float[] inputs;

    enum actions{ stop, foward, backward, turn_left, turn_right }
    float timer = 0;
    int episode_count = 0;
    int step_count = 0;
    int step_passed = 0;
    bool wait_for_start = true;
    float travel_distance = 0.0f;

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

        if(timer > 1.0f/framerate){
            if(wait_for_start && timer < 1.0f){
                cam_script.Capture();
                distances = cam_script.GetDistances();
                for (int i=0; i<5; i++){
                    inputs[i] = distances[i];
                }
                inputs[5] = travel_distance;
                q_script.Reset(inputs);
                wait_for_start = false;
                return;
            } 
            timer = 0;

            if ( episode_count < episodes )
            {
                cam_script.Capture();
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
                //float reward = (left_wheel_torque/motor_torque) + (right_wheel_torque/motor_torque) + reward_distances;
                float reward = (travel_distance + reward_distances - ((float)step_passed * 0.01f));
                text_reward.text = reward.ToString("0.000");

                actions next_action = (actions)q_script.NextStepTrain(inputs, reward, done);

                if (!done)
                {
                    switch(next_action)
                    {
                    case actions.foward:
                        left_wheel_torque = motor_torque;
                        right_wheel_torque = motor_torque;
                        travel_distance += 0.1f;
                        break;
                    case actions.backward:
                        left_wheel_torque = -motor_torque;
                        right_wheel_torque = -motor_torque;
                        travel_distance -= 0.1f;
                        break;
                    case actions.turn_left:
                        left_wheel_torque = -motor_torque;
                        right_wheel_torque = motor_torque;
                        break;
                    case actions.turn_right:
                        left_wheel_torque = motor_torque;
                        right_wheel_torque = -motor_torque;
                        break;
                    case actions.stop:
                        left_wheel_torque = 0.0f;
                        right_wheel_torque = 0.0f;
                        break;
                    }

                    foreach (WheelCollider wheel in leftWheels){
                        wheel.motorTorque = left_wheel_torque;
                    }
                    foreach (WheelCollider wheel in rightWheels){
                        wheel.motorTorque = right_wheel_torque;
                    }

                    if (step_count > 20){
                        q_script.ReplayTrain();
                        step_count = 0;
                    }
                    step_count++;
                    step_passed++;
                }
                else
                {
                    // Replay and Training
                    q_script.ReplayTrain();

                    // Reset environment
                    left_wheel_torque = 0.0f;
                    right_wheel_torque = 0.0f;
                    travel_distance = 0.0f;
                    rigidbody.velocity = Vector2.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    transform.position = start_position.position;
                    transform.rotation = start_position.rotation;
                    q_script.Reset(distances);
                    step_passed = 0;
                    episode_count++;
                }
            }
            else
            {
                // All Episodes is End

            }

            //Debug.Log(action_best);
        }
    }
}
