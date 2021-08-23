local WaveScript = {}

local m_Time
local m_WaveX
local m_WaveY
local m_WaveZ
local m_WaveW

local m_OutWaveX
local m_OutWaveY
local m_OutWaveZ
local m_OutWaveW

function WaveScript.onInit(self, graph)
    graph:updateProperties({name = "Wave"})

    -- create input linkers
    local inputs = graph:setInputLinkers
    {
    	{
            type            = octane.PT_FLOAT,
            label           = "Time",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = 0,
        },
        {
            type            = octane.PT_FLOAT,
            label           = "X",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = 0,
        },
        {
            type            = octane.PT_FLOAT,
            label           = "Y",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = 0,
        },
        {
            type            = octane.PT_FLOAT,
            label           = "Z",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = 0,
        },
                {
            type            = octane.PT_FLOAT,
            label           = "W",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = 0,
        }

    }

    mTime = inputs[1]
	mWaveX = inputs[2]
	mWaveY = inputs[3]
	mWaveZ = inputs[4]
	mWaveW = inputs[5]
        
    -- create output linker
   local outputs = graph:setOutputLinkers
    {
        	        {
            type            = octane.PT_FLOAT,
            label           = "X",
            defaultNodeType = octane.NT_OUT_FLOAT,
            defaultValue    = 0,
        },
        {
            type            = octane.PT_FLOAT,
            label           = "Y",
            defaultNodeType = octane.NT_OUT_FLOAT,
            defaultValue    = 0,
        },
        {
            type            = octane.PT_FLOAT,
            label           = "Z",
            defaultNodeType = octane.NT_OUT_FLOAT,
            defaultValue    = 0,
        },
                {
            type            = octane.PT_FLOAT,
            label           = "W",
            defaultNodeType = octane.NT_OUT_FLOAT,
            defaultValue    = 0,
        }
    }

    mOutWaveX = outputs[1]
    mOutWaveY = outputs[2]
    mOutWaveZ = outputs[3]
    mOutWaveW = outputs[4]
 
end


function AnimateValueScript.onEvaluate(self, graph)
    local x  = mWaveX:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
    local y  = mWaveY:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
    local z  = mWaveZ:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
    local w  = mWaveW:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)

    
end


return WaveScript
