//using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

struct ReplayMemory
{
    public float[] states;
    public int action;
    public float reward;
    public float[] next_states;
    public bool done;
}

public class DeepQ : MonoBehaviour
{
    public int num_strcture = 5;
    public int[] num_nodes;

    public float learning_rate = 0.1f;
    public float discount_factor = 0.8f;
    public float epsilon_factor = 0.1f;
    public int max_replay_buffer = 10000;
    public int max_replay_batch = 32;

    List<float[]> nodes_list;
    List<float[]> errors_list;
    List<float[,]> weights_list;
    List<float[,]> weights_update_list;
    //List<int> num_weights;
    float[] states;
    int debug_i = 5;
    bool debug_b = true;

    ArrayList replay_buffer;

    // Start is called before the first frame update
    void Start()
    {
        //num_weights = new List<int>(new int[num_strcture - 1]);
        nodes_list = new List<float[]>();
        errors_list = new List<float[]>();
        weights_list = new List<float[,]>();
        weights_update_list = new List<float[,]>();
        replay_buffer = new ArrayList();
        replay_buffer.Capacity = max_replay_buffer;

        for (int i=0; i<num_strcture; i++){
            float[] nodes = new float[num_nodes[i]];
            nodes_list.Add((float[])nodes.Clone());
            errors_list.Add((float[])nodes.Clone());
        }

        // He Weight Initialization
        //var r = new System.Random();
        for (int i=0; i<num_strcture - 1; i++){
            float[,] weights = new float[num_nodes[i], num_nodes[i + 1]];
            float deviation = (float)Mathf.Sqrt(2.0f/num_nodes[i]);
            for (int j=0; j<num_nodes[i]; j++){
                for  (int k=0; k<num_nodes[i + 1]; k++){
                    weights[j, k] = GaussianRandom(0.0f, deviation);
                }
            }
            weights_list.Add((float[,])weights.Clone());
            weights_update_list.Add((float[,])weights.Clone());
        }
    }

    // Initiallize for New Episode
    public void Reset(float[] states_){
        states = (float[])states_.Clone();
    }

    // Main Step Algorism for Train
    public int NextStepTrain(float[] next_states, float reward, bool done)
    {
        Prediction(states);

        ///////////////// Select Action using Epsilon Greedy /////////////////
        int action;
        if ( Random.Range(0.0f, 1.0f) < epsilon_factor){
            // Random Action
            action = Random.Range(0, num_nodes[num_strcture-1]);
        }
        else{
            // Greedy Action
            float max_val = nodes_list[num_strcture - 1].Max();
            action = nodes_list[num_strcture - 1].ToList().IndexOf(max_val);
        }

        ///////////////// Write for Replay /////////////////
        Remember(states, action, reward, next_states, done);

        states = (float[])next_states.Clone();
        return action;
    }

    // Replay and Training
    public void ReplayTrain()
    {
        if(replay_buffer.Count > max_replay_batch)
        {
            for (int i=0; i<num_nodes[num_strcture-1]; i++){
                Debug.Log(nodes_list[num_strcture-1][i]);
            }

            // Using Random Batches
            int index = Random.Range(0, replay_buffer.Count - max_replay_batch);
            for (int i = 0; i<max_replay_batch; i++){
                ReplayMemory replay_ = (ReplayMemory)replay_buffer[index + i];
                float target = replay_.reward;
                if (!replay_.done){
                    Prediction(replay_.next_states);
                    target += discount_factor * nodes_list[num_strcture - 1].Max();
                }
                Prediction(replay_.states);
                float[] target_nodes = (float[])nodes_list[num_strcture - 1].Clone();
                target_nodes[replay_.action] = target;
                BackPropagation(target_nodes, replay_.action);
            }
        }
    }

    // Back Propagation and Update weights
    void BackPropagation(float[] target_initial, int debug_)
    {
        // Every nodes should be arranged by prediction() before calling this function
        // Assume Relu function as activation function
        // loss = power(target - prediction, 2)/2
        // Gradient descent
        
        // for each layer start from the back except input layer
        for (int i=num_strcture-1; i>0; i--)
        {
            // for each node
            for (int j=0; j<num_nodes[i]; j++)
            {
                // Calculate derivative of loss
                if (i == num_strcture-1){ 
                    // Final layer
                    errors_list[i][j] = (nodes_list[i][j] - target_initial[j]);
                }
                else{
                    // hidden layer
                    errors_list[i][j] = 0;
                    for (int k=0; k<num_nodes[i+1]; k++){
                        errors_list[i][j] += errors_list[i+1][k] * weights_list[i][j, k];
                    }
                }
                // derivative of Relu
                if (nodes_list[i][j] == 0){
                    errors_list[i][j] = 0f;
                }
                // for each weight
                for (int k=0; k<num_nodes[i-1]; k++){
                    weights_update_list[i-1][k, j] = errors_list[i][j] * nodes_list[i-1][k];
                }
            }
        }

        // Update
        for (int i=1; i<num_strcture; i++)
            for (int j=0; j<num_nodes[i]; j++)
                for  (int k=0; k<num_nodes[i-1]; k++){
                    //if(debug_b){
                    //    debug_b = false;
                    //    Debug.Log(weights_list[i-1][k, j]);
                    //    Debug.Log(weights_update_list[i-1][k, j]);
                    //}
                    weights_list[i-1][k, j] -= learning_rate * weights_update_list[i-1][k, j];
                }
    }

    // Forward Propagation Using Current States
    void Prediction(float[] states_)
    {
        nodes_list[0] = (float[])states_.Clone();
        // for each layer except input layer
        for (int i=1; i<num_strcture; i++){
            // for each node
            for (int j=0; j<num_nodes[i]; j++){
                nodes_list[i][j] = 0;
                // for each weight
                for (int k=0; k<num_nodes[i-1]; k++){
                    nodes_list[i][j] += nodes_list[i-1][k] * weights_list[i-1][k, j];
                }
                //RELU Function
                nodes_list[i][j] = (nodes_list[i][j] > 0) ? nodes_list[i][j] : 0;
            }
        }
    }

    // Gaussian Random
    static float GaussianRandom(float mu, float sigma)
    {
        float rand1 = Random.Range(0.0f, 1.0f);
        float rand2 = Random.Range(0.0f, 1.0f);
        float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos((2.0f * Mathf.PI) * rand2);
        return (mu + sigma * n);
    }

    // Safe Enqueue for replay_buffer
    void Remember(float[] states, int action, float reward, float[] next_states, bool done)
    {
        if (replay_buffer.Count > max_replay_buffer-2){
            replay_buffer.RemoveAt(0);
        }
        ReplayMemory rm;
        rm.states = (float[])states.Clone();
        rm.action = action;
        rm.reward = reward;
        rm.next_states = (float[])next_states.Clone();
        rm.done = done;
        replay_buffer.Add(rm);
    }

    // Update is called once per frame
    void Update()
    {   
    }
}
