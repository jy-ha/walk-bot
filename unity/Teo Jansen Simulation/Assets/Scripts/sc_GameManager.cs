using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class sc_GameManager : MonoBehaviour
{
    public GameObject robot_prefab;
    public Transform start_location;
    public float learning_rate = 0.01f;
    public Vector3 camera_offset = new Vector3(0.3f, 0.0f, -10.0f);
    Transform main_camera;

    float timer = 0;

    bool b_round = false;
    bool b_motor = false;
    bool b_testing = false;

    Text[] text_links_front = new Text[11];
    Text[] text_axis_front = new Text[2];
    Text[] text_links_rear = new Text[11];
    Text[] text_axis_rear = new Text[2];
    Text text_round;
    Text text_reward;
    Text text_size_x;
    Text text_size_y;
    Text text_speed;
    Text text_delta_v;
    Text text_delta_h;

    int round = 0;
    int tick_cnt = 0;
    float reward = 0.0f;
    float reward_last = 0.0f;
    float speed_low = 1000.0f;
    float speed_high = 0.0f;
    float speed_delta = 0.0f;
    float speed_avg = 0.0f;
    float speed_tot = 0.0f;
    float height_low = 1000.0f;
    float height_high = -1000.0f;
    float height_delta = 0.0f;
    float[] links = new float[26];
    //float[] axis = new float[2];

    // dimension
    Transform body_transform;
    Transform edge_left;
    Transform edge_right;
    Transform edge_top;
    Transform edge_bottom;
    float max_width = 0.0f;
    float max_height = 0.0f;

    // components
    Transform[] legs_1 = new Transform[11];
    Transform[] legs_2 = new Transform[11];
    Transform[] legs_3 = new Transform[11];
    Transform[] legs_4 = new Transform[11];
    Transform[] legs_5 = new Transform[11];
    Transform[] legs_6 = new Transform[11];
    HingeJoint2D[] legs_axis = new HingeJoint2D[6];
    Rigidbody2D body_rigid;
    HingeJoint2D motor;

    // legs
    bool b_find_new = false;
    int leg_selected_index = 1;
    bool b_leg_extending = false;
    int leg_iteration_cnt = 0;
    float leg_iteration_tot = 0.0f;
    float leg_iteration_avg_last = 0.0f;
    float leg_scale_reference = 15.0f;
    float decay_factor_time = 1.0f;
    float decay_factor_damp = 1.0f;



    // Start is called before the first frame update
    void Start()
    {
        initiallize();
        
        b_round = true;
        get_components();
    }

    // Update is called once per frame
    void Update()
    {
        if (b_round)
        {
            timer += Time.deltaTime;

            if (!b_motor && timer > 0.5f) {
                motor.useMotor = true;
                b_motor = true;
            }
            if (b_motor) main_camera.position = body_transform.position + camera_offset;

            if (!b_testing && timer > 1.5f) {
                b_testing = true;
            }

            // End Round
            if (timer > 5.0f)
            {
                b_round = false;
                round++;
                decay_factor_time *= 0.9995f;
                text_round.text = round.ToString();

                // calculate reward
                reward += speed_avg;
                if (max_width > 300 || max_height > 300) reward -= 100;
                //reward -= speed_delta;
                reward -= height_delta * height_delta * height_delta * 2;
                text_reward.text = reward.ToString();

                float leg_scale_factor = links[leg_selected_index] / leg_scale_reference;
                float max_cliping = leg_scale_factor * learning_rate * decay_factor_time;

                if (reward_last != 0.0f) // not a first round
                {
                    leg_iteration_cnt++;

                    float reward_delta = reward - reward_last;
                    reward_last = reward;
                    if (reward_delta < 0.0f) {  // toggle direction
                        if (b_leg_extending){
                            b_leg_extending = false;
                            decay_factor_damp *= 0.8f;
                        }
                        else {
                            b_leg_extending = true;
                            decay_factor_damp *= 0.8f;
                        }
                    }

                    
                    float leg_len_delta = Mathf.Abs(learning_rate * reward_delta) * leg_scale_factor * decay_factor_time * decay_factor_damp * 10;
                    if (leg_len_delta > max_cliping) leg_len_delta = max_cliping;
                    float leg_iteration_avg = leg_iteration_tot / leg_iteration_cnt;
                    float leg_iteration_avg_delta = Mathf.Abs(leg_iteration_avg_last - leg_iteration_avg);
                    leg_iteration_avg_last = leg_iteration_avg;
                    //Debug.Log(leg_iteration_avg);

                    //if (leg_iteration_avg_delta > leg_scale_factor * decay_factor_time * 0.001f)
                    if(leg_len_delta > 0.003f * leg_scale_factor * decay_factor_time)
                    {
                        if (b_leg_extending) links[leg_selected_index] += leg_len_delta;
                        else links[leg_selected_index] -= leg_len_delta;
                        leg_iteration_tot += links[leg_selected_index];
                    }
                    else
                    {
                        //links[leg_selected_index] = leg_iteration_avg;
                        b_find_new = true;
                    }
                }
                else
                {
                    leg_iteration_cnt = 0;
                    leg_iteration_avg_last = links[leg_selected_index];
                    links[leg_selected_index] -= max_cliping;
                    leg_iteration_tot = links[leg_selected_index];
                    reward_last = reward;
                    decay_factor_damp = 1.0f;
                }

                Destroy(GameObject.Find("Robot"));

                GameObject new_bot = Instantiate(robot_prefab, start_location.position, Quaternion.identity);
                new_bot.name = "Robot";

                
            }

            if (b_testing)
            {
                tick_cnt++;

                float velocity = body_rigid.velocity.x * 100;
                //Debug.Log(velocity);
                speed_tot += velocity;
                speed_avg = speed_tot / tick_cnt;
                text_speed.text = speed_avg.ToString("0.0");
                
                // Dimension check
                float width = (edge_right.position.x - edge_left.position.x) * 1000;
                float height = (edge_top.position.y - edge_bottom.position.y) * 1000;
                if (max_width < width){
                    max_width = width;
                    text_size_x.text = max_width.ToString("0");
                }
                if (max_height < height){
                    max_height = height;
                    text_size_y.text = max_height.ToString("0");
                }

                // Delta Speed check
                if(speed_low > velocity){
                    speed_low = velocity;
                    speed_delta = speed_high - speed_low;
                    text_delta_v.text = speed_delta.ToString("0.0");
                }
                if (speed_high < velocity){
                    speed_high = velocity;
                    speed_delta = speed_high - speed_low;
                    text_delta_v.text = speed_delta.ToString("0.0");
                }

                // Delta Height check
                if (height_low > body_transform.position.y){
                    height_low = body_transform.position.y;
                    height_delta = Mathf.Abs(height_high - height_low) * 1000;
                    text_delta_h.text = height_delta.ToString("0.00");
                }
                if (height_high < body_transform.position.y){
                    height_high = body_transform.position.y;
                    height_delta = Mathf.Abs(height_high - height_low) * 1000;
                    text_delta_h.text = height_delta.ToString("0.00");
                }
            }
            
        }
        else
        {
            get_components();

            timer = 0;
            tick_cnt = 0;
            b_round = true;
            b_motor = false;
            b_testing = false;
            max_width = 0.0f;
            max_height = 0.0f;
            reward = 0.0f;
            speed_low = 1000.0f;
            speed_high = 0.0f;
            speed_delta = 0.0f;
            speed_avg = 0.0f;
            speed_tot = 0.0f;
            height_low = 1000.0f;
            height_high = -1000.0f;
            height_delta = 0.0f;

            if (b_find_new)
            {
                reward_last = 0.0f;
                leg_selected_index++;
                if (leg_selected_index == 26) leg_selected_index = 1;
                if (leg_selected_index == 13) leg_selected_index = 14;
                //leg_selected_index = Random.Range(1, 26);
                b_find_new = false;
            }

            // change leg size
            for (int i = 0; i < 11; i++) legs_1[i].localScale = new Vector3(1.0f, links[i] / 10, 1.0f);
            for (int i = 0; i < 11; i++) legs_2[i].localScale = new Vector3(1.0f, links[i] / 10, 1.0f);
            for (int i = 0; i < 11; i++) legs_3[i].localScale = new Vector3(1.0f, links[i] / 10, 1.0f);

            for (int i = 0; i < 11; i++) legs_4[i].localScale = new Vector3(1.0f, links[13 + i] / 10, 1.0f);
            for (int i = 0; i < 11; i++) legs_5[i].localScale = new Vector3(1.0f, links[13 + i] / 10, 1.0f);
            for (int i = 0; i < 11; i++) legs_6[i].localScale = new Vector3(1.0f, links[13 + i] / 10, 1.0f);

            for (int i = 0; i < 3; i++) legs_axis[i].connectedAnchor = new Vector2(links[11] / -500, links[12] / -500);
            for (int i = 3; i < 6; i++) legs_axis[i].connectedAnchor = new Vector2(links[24] / 500, links[25] / -500);

            for (int i = 0; i < 11; i++) text_links_rear[i].text = links[i].ToString("0.0000");
            for (int i = 0; i < 2; i++) text_axis_rear[i].text = links[11 + i].ToString("0.0000");
            for (int i = 0; i < 11; i++) text_links_front[i].text = links[13 + i].ToString("0.0000");
            for (int i = 0; i < 2; i++) text_axis_front[i].text = links[24 + i].ToString("0.0000");
        }
    }

    void get_components()
    {
        motor = GameObject.Find("Robot/motor").GetComponent<HingeJoint2D>();
        body_rigid = GameObject.Find("Robot/body").GetComponent<Rigidbody2D>();
        body_transform = GameObject.Find("Robot/body").transform;
        edge_left = GameObject.Find("Robot/leg/linkage (8)").transform;
        edge_right = GameObject.Find("Robot/leg (3)/linkage (8)").transform;
        edge_top = GameObject.Find("Robot/leg/linkage (2)").transform;
        edge_bottom = GameObject.Find("Robot/leg/linkage (8)/Cylinder").transform;

        legs_1[0] = GameObject.Find("Robot/leg/crank").transform;
        legs_1[1] = GameObject.Find("Robot/leg/linkage").transform;
        legs_1[2] = GameObject.Find("Robot/leg/linkage (1)").transform;
        legs_1[3] = GameObject.Find("Robot/leg/linkage (2)").transform;
        legs_1[4] = GameObject.Find("Robot/leg/linkage (3)").transform;
        legs_1[5] = GameObject.Find("Robot/leg/linkage (4)").transform;
        legs_1[6] = GameObject.Find("Robot/leg/linkage (5)").transform;
        legs_1[7] = GameObject.Find("Robot/leg/linkage (6)").transform;
        legs_1[8] = GameObject.Find("Robot/leg/linkage (7)").transform;
        legs_1[9] = GameObject.Find("Robot/leg/linkage (8)").transform;
        legs_1[10] = GameObject.Find("Robot/leg/linkage (9)").transform;

        legs_2[0] = GameObject.Find("Robot/leg (1)/crank").transform;
        legs_2[1] = GameObject.Find("Robot/leg (1)/linkage").transform;
        legs_2[2] = GameObject.Find("Robot/leg (1)/linkage (1)").transform;
        legs_2[3] = GameObject.Find("Robot/leg (1)/linkage (2)").transform;
        legs_2[4] = GameObject.Find("Robot/leg (1)/linkage (3)").transform;
        legs_2[5] = GameObject.Find("Robot/leg (1)/linkage (4)").transform;
        legs_2[6] = GameObject.Find("Robot/leg (1)/linkage (5)").transform;
        legs_2[7] = GameObject.Find("Robot/leg (1)/linkage (6)").transform;
        legs_2[8] = GameObject.Find("Robot/leg (1)/linkage (7)").transform;
        legs_2[9] = GameObject.Find("Robot/leg (1)/linkage (8)").transform;
        legs_2[10] = GameObject.Find("Robot/leg (1)/linkage (9)").transform;

        legs_3[0] = GameObject.Find("Robot/leg (2)/crank").transform;
        legs_3[1] = GameObject.Find("Robot/leg (2)/linkage").transform;
        legs_3[2] = GameObject.Find("Robot/leg (2)/linkage (1)").transform;
        legs_3[3] = GameObject.Find("Robot/leg (2)/linkage (2)").transform;
        legs_3[4] = GameObject.Find("Robot/leg (2)/linkage (3)").transform;
        legs_3[5] = GameObject.Find("Robot/leg (2)/linkage (4)").transform;
        legs_3[6] = GameObject.Find("Robot/leg (2)/linkage (5)").transform;
        legs_3[7] = GameObject.Find("Robot/leg (2)/linkage (6)").transform;
        legs_3[8] = GameObject.Find("Robot/leg (2)/linkage (7)").transform;
        legs_3[9] = GameObject.Find("Robot/leg (2)/linkage (8)").transform;
        legs_3[10] = GameObject.Find("Robot/leg (2)/linkage (9)").transform;

        legs_4[0] = GameObject.Find("Robot/leg (3)/crank").transform;
        legs_4[1] = GameObject.Find("Robot/leg (3)/linkage").transform;
        legs_4[2] = GameObject.Find("Robot/leg (3)/linkage (1)").transform;
        legs_4[3] = GameObject.Find("Robot/leg (3)/linkage (2)").transform;
        legs_4[4] = GameObject.Find("Robot/leg (3)/linkage (3)").transform;
        legs_4[5] = GameObject.Find("Robot/leg (3)/linkage (4)").transform;
        legs_4[6] = GameObject.Find("Robot/leg (3)/linkage (5)").transform;
        legs_4[7] = GameObject.Find("Robot/leg (3)/linkage (6)").transform;
        legs_4[8] = GameObject.Find("Robot/leg (3)/linkage (7)").transform;
        legs_4[9] = GameObject.Find("Robot/leg (3)/linkage (8)").transform;
        legs_4[10] = GameObject.Find("Robot/leg (3)/linkage (9)").transform;

        legs_5[0] = GameObject.Find("Robot/leg (4)/crank").transform;
        legs_5[1] = GameObject.Find("Robot/leg (4)/linkage").transform;
        legs_5[2] = GameObject.Find("Robot/leg (4)/linkage (1)").transform;
        legs_5[3] = GameObject.Find("Robot/leg (4)/linkage (2)").transform;
        legs_5[4] = GameObject.Find("Robot/leg (4)/linkage (3)").transform;
        legs_5[5] = GameObject.Find("Robot/leg (4)/linkage (4)").transform;
        legs_5[6] = GameObject.Find("Robot/leg (4)/linkage (5)").transform;
        legs_5[7] = GameObject.Find("Robot/leg (4)/linkage (6)").transform;
        legs_5[8] = GameObject.Find("Robot/leg (4)/linkage (7)").transform;
        legs_5[9] = GameObject.Find("Robot/leg (4)/linkage (8)").transform;
        legs_5[10] = GameObject.Find("Robot/leg (4)/linkage (9)").transform;

        legs_6[0] = GameObject.Find("Robot/leg (5)/crank").transform;
        legs_6[1] = GameObject.Find("Robot/leg (5)/linkage").transform;
        legs_6[2] = GameObject.Find("Robot/leg (5)/linkage (1)").transform;
        legs_6[3] = GameObject.Find("Robot/leg (5)/linkage (2)").transform;
        legs_6[4] = GameObject.Find("Robot/leg (5)/linkage (3)").transform;
        legs_6[5] = GameObject.Find("Robot/leg (5)/linkage (4)").transform;
        legs_6[6] = GameObject.Find("Robot/leg (5)/linkage (5)").transform;
        legs_6[7] = GameObject.Find("Robot/leg (5)/linkage (6)").transform;
        legs_6[8] = GameObject.Find("Robot/leg (5)/linkage (7)").transform;
        legs_6[9] = GameObject.Find("Robot/leg (5)/linkage (8)").transform;
        legs_6[10] = GameObject.Find("Robot/leg (5)/linkage (9)").transform;

        legs_axis[0] = GameObject.Find("Robot/leg/linkage (4)").GetComponent<HingeJoint2D>();
        legs_axis[1] = GameObject.Find("Robot/leg (1)/linkage (4)").GetComponent<HingeJoint2D>();
        legs_axis[2] = GameObject.Find("Robot/leg (2)/linkage (4)").GetComponent<HingeJoint2D>();
        legs_axis[3] = GameObject.Find("Robot/leg (3)/linkage (4)").GetComponent<HingeJoint2D>();
        legs_axis[4] = GameObject.Find("Robot/leg (4)/linkage (4)").GetComponent<HingeJoint2D>();
        legs_axis[5] = GameObject.Find("Robot/leg (5)/linkage (4)").GetComponent<HingeJoint2D>();
    }

    void initiallize()
    {
        main_camera = GameObject.Find("Main Camera").transform;

        text_links_rear[0] = GameObject.Find("UI/crank/Value").GetComponent<Text>();
        text_links_rear[1] = GameObject.Find("UI/link 0/Value").GetComponent<Text>();
        text_links_rear[2] = GameObject.Find("UI/link 1/Value").GetComponent<Text>();
        text_links_rear[3] = GameObject.Find("UI/link 2/Value").GetComponent<Text>();
        text_links_rear[4] = GameObject.Find("UI/link 3/Value").GetComponent<Text>();
        text_links_rear[5] = GameObject.Find("UI/link 4/Value").GetComponent<Text>();
        text_links_rear[6] = GameObject.Find("UI/link 5/Value").GetComponent<Text>();
        text_links_rear[7] = GameObject.Find("UI/link 6/Value").GetComponent<Text>();
        text_links_rear[8] = GameObject.Find("UI/link 7/Value").GetComponent<Text>();
        text_links_rear[9] = GameObject.Find("UI/link 8/Value").GetComponent<Text>();
        text_links_rear[10] = GameObject.Find("UI/link 9/Value").GetComponent<Text>();
        text_axis_rear[0] = GameObject.Find("UI/axis_x/Value").GetComponent<Text>();
        text_axis_rear[1] = GameObject.Find("UI/axis_y/Value").GetComponent<Text>();
        text_links_front[0] = GameObject.Find("UI/crank (1)/Value").GetComponent<Text>();
        text_links_front[1] = GameObject.Find("UI/link 0 (1)/Value").GetComponent<Text>();
        text_links_front[2] = GameObject.Find("UI/link 1 (1)/Value").GetComponent<Text>();
        text_links_front[3] = GameObject.Find("UI/link 2 (1)/Value").GetComponent<Text>();
        text_links_front[4] = GameObject.Find("UI/link 3 (1)/Value").GetComponent<Text>();
        text_links_front[5] = GameObject.Find("UI/link 4 (1)/Value").GetComponent<Text>();
        text_links_front[6] = GameObject.Find("UI/link 5 (1)/Value").GetComponent<Text>();
        text_links_front[7] = GameObject.Find("UI/link 6 (1)/Value").GetComponent<Text>();
        text_links_front[8] = GameObject.Find("UI/link 7 (1)/Value").GetComponent<Text>();
        text_links_front[9] = GameObject.Find("UI/link 8 (1)/Value").GetComponent<Text>();
        text_links_front[10] = GameObject.Find("UI/link 9 (1)/Value").GetComponent<Text>();
        text_axis_front[0] = GameObject.Find("UI/axis_x (1)/Value").GetComponent<Text>();
        text_axis_front[1] = GameObject.Find("UI/axis_y (1)/Value").GetComponent<Text>();
        text_round = GameObject.Find("UI/round/Value").GetComponent<Text>();
        text_reward = GameObject.Find("UI/reward/Value").GetComponent<Text>();
        text_size_x = GameObject.Find("UI/size/Value_x").GetComponent<Text>();
        text_size_y = GameObject.Find("UI/size/Value_y").GetComponent<Text>();
        text_speed = GameObject.Find("UI/speed/Value").GetComponent<Text>();
        text_delta_v = GameObject.Find("UI/delta_v/Value").GetComponent<Text>();
        text_delta_h = GameObject.Find("UI/delta_h/Value").GetComponent<Text>();

        links[0] = GameObject.Find("Robot/leg/crank").transform.localScale.y * 10;
        links[1] = GameObject.Find("Robot/leg/linkage").transform.localScale.y * 10;
        links[2] = GameObject.Find("Robot/leg/linkage (1)").transform.localScale.y * 10;
        links[3] = GameObject.Find("Robot/leg/linkage (2)").transform.localScale.y * 10;
        links[4] = GameObject.Find("Robot/leg/linkage (3)").transform.localScale.y * 10;
        links[5] = GameObject.Find("Robot/leg/linkage (4)").transform.localScale.y * 10;
        links[6] = GameObject.Find("Robot/leg/linkage (5)").transform.localScale.y * 10;
        links[7] = GameObject.Find("Robot/leg/linkage (6)").transform.localScale.y * 10;
        links[8] = GameObject.Find("Robot/leg/linkage (7)").transform.localScale.y * 10;
        links[9] = GameObject.Find("Robot/leg/linkage (8)").transform.localScale.y * 10;
        links[10] = GameObject.Find("Robot/leg/linkage (9)").transform.localScale.y * 10;
        links[13] = GameObject.Find("Robot/leg (3)/crank").transform.localScale.y * 10;
        links[14] = GameObject.Find("Robot/leg (3)/linkage").transform.localScale.y * 10;
        links[15] = GameObject.Find("Robot/leg (3)/linkage (1)").transform.localScale.y * 10;
        links[16] = GameObject.Find("Robot/leg (3)/linkage (2)").transform.localScale.y * 10;
        links[17] = GameObject.Find("Robot/leg (3)/linkage (3)").transform.localScale.y * 10;
        links[18] = GameObject.Find("Robot/leg (3)/linkage (4)").transform.localScale.y * 10;
        links[19] = GameObject.Find("Robot/leg (3)/linkage (5)").transform.localScale.y * 10;
        links[20] = GameObject.Find("Robot/leg (3)/linkage (6)").transform.localScale.y * 10;
        links[21] = GameObject.Find("Robot/leg (3)/linkage (7)").transform.localScale.y * 10;
        links[22] = GameObject.Find("Robot/leg (3)/linkage (8)").transform.localScale.y * 10;
        links[23] = GameObject.Find("Robot/leg (3)/linkage (9)").transform.localScale.y * 10;
        
        links[11] = GameObject.Find("Robot/leg/linkage (4)").GetComponent<HingeJoint2D>().connectedAnchor.x * -500;
        links[12] = GameObject.Find("Robot/leg/linkage (4)").GetComponent<HingeJoint2D>().connectedAnchor.y * -500;
        links[24] = GameObject.Find("Robot/leg (3)/linkage (4)").GetComponent<HingeJoint2D>().connectedAnchor.x * 500;
        links[25] = GameObject.Find("Robot/leg (3)/linkage (4)").GetComponent<HingeJoint2D>().connectedAnchor.y * -500;

        for (int i = 0; i < 11; i++) text_links_rear[i].text = links[i].ToString("0.0000");
        for (int i = 0; i < 2; i++) text_axis_rear[i].text = links[11 + i].ToString("0.0000");
        for (int i = 0; i < 11; i++) text_links_front[i].text = links[13 + i].ToString("0.0000");
        for (int i = 0; i < 2; i++) text_axis_front[i].text = links[24 + i].ToString("0.0000");
    }
}
