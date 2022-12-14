using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{
    // Start is called before the first frame update
    public RenderTexture renderTexture;
    public ComputeShader displayShader;
    public ComputeShader agentShader;
    private ComputeBuffer _agentBuffer;
    private ComputeBuffer _debugBuffer;
    private RenderTexture _trailMap;
    public int numAgents;
    public Color agentColor;
    public int agentRadius;
    public int width;
    public int height;
    public float minAgentVelocity;
    public float maxAgentVelocity;
    public float decayRate;
    public float blurrRatio;
    public float agentFOV;
    public float agentTurnRatio;
    
    struct Agent
    {
        public Vector2 position;
        public Vector2 normalizedDirection;
        public float velocity;
    }
    
    struct DebugElement
    {
        public Vector2 evaluatedPosition;
    };

    private Agent[] _agents;
    private DebugElement[] _debugs;
    private void Start()
    {
        SetupTextures();

        SetupBuffers();

        SetupDisplayShader();
        
        SetupAgentShader();
        
        transform.GetComponent<RawImage>().texture = renderTexture;
        GameObject.Find("Mask").GetComponent<RawImage>().texture = _trailMap;
    }

    private void SetupTextures()
    {
        renderTexture = new RenderTexture(width, height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        
        _trailMap = new RenderTexture(width, height, 24);
        _trailMap.enableRandomWrite = true;
        _trailMap.Create();
    }

    private void SetupBuffers()
    {
        _agents = new Agent[numAgents];
        PopulateAgents();

        _debugs = new DebugElement[50];
        
        const int structSize = sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float);
        _agentBuffer = new ComputeBuffer(numAgents, structSize);
        _agentBuffer.SetData(_agents);
        
        const int debugStructSize = sizeof(float) * 2;
        _debugBuffer = new ComputeBuffer(50, debugStructSize);
        _debugBuffer.SetData(_debugs);
    }

    private void SetupDisplayShader()
    {
        displayShader.SetBuffer(1, "agents", _agentBuffer);
        
        displayShader.SetInt("agentRadius", agentRadius);
        displayShader.SetFloat("shift", 0);
        displayShader.SetVector("color", agentColor);
        
        displayShader.SetTexture(0, "Result", renderTexture);
        displayShader.SetTexture(2, "trailMap", _trailMap);
        displayShader.SetTexture(1, "Result", renderTexture);
        
        displayShader.Dispatch(0, width/8, height/8, 1);
        displayShader.Dispatch(2, width/8, height/8, 1);
        displayShader.Dispatch(1, numAgents/10, 1, 1);
    }

    private void SetupAgentShader()
    {
        agentShader.SetBuffer(0, "debugs", _debugBuffer);
        agentShader.SetBuffer(1, "debugs", _debugBuffer);
        agentShader.SetBuffer(0, "agents", _agentBuffer);
        agentShader.SetBuffer(2, "agents", _agentBuffer);
        
        agentShader.SetFloat("width", width);
        agentShader.SetFloat("height", height);
        agentShader.SetFloat("blurrRatio", blurrRatio);
        agentShader.SetFloat("agentTurnRatio", agentTurnRatio);
        agentShader.SetFloat("agentFOV", agentFOV);
        agentShader.SetFloat("decayRate", decayRate);
        agentShader.SetFloat("width", width);
        agentShader.SetFloat("height", height);
        agentShader.SetVector("agentColor", agentColor);
        
        agentShader.SetTexture(0, "trailMap", _trailMap);
        agentShader.SetTexture(1, "trailMap", _trailMap);
        agentShader.SetTexture(2, "trailMap", _trailMap);
    }

    private void OnValidate()
    {
        agentShader.SetFloat("blurrRatio", blurrRatio);
        agentShader.SetFloat("agentTurnRatio", agentTurnRatio);
        agentShader.SetFloat("decayRate", decayRate);
        agentShader.SetFloat("agentFOV", agentFOV);
    }

    private void PopulateAgents()
    {
        for (int i = 0; i < numAgents; i++)
        {
            _agents[i] = new Agent()
            {
                position = new Vector2(Random.Range(0.0f, width), Random.Range(0.0f, height)),
                normalizedDirection = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)),
                velocity = Random.Range(minAgentVelocity, maxAgentVelocity)
            };
        }
    }
    private void FixedUpdate()
    {
        ProcessAgentShader();
        ProcessTextureShader();
    }

    private void ProcessAgentShader()
    {
        agentShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        agentShader.Dispatch(0, numAgents/10, 1, 1);
        agentShader.Dispatch(1, width/8, height/8, 1);
        /*_debugBuffer.GetData(_debugs);
        Debug.Log("Frame :");
        foreach (var debug in _debugs)
        {
            Debug.Log(debug.evaluatedPosition);
        }*/
        agentShader.Dispatch(2, numAgents/10, 1, 1);
    }

    private void ProcessTextureShader()
    {
        displayShader.Dispatch(0, width / 8, height / 8, 1);
        displayShader.Dispatch(1, numAgents/10, 1, 1);
    }
}
