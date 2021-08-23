local WetRoadScript = {}

local m_Wetness
local m_WaterLevel
local m_Darken
local m_Specular
local m_Smoothness
local m_PuddleFill
local m_PuddleIntensity

MIN_DIFFUSE_GAMMA = 1.5 --dry-wet
MAX_DIFFUSE_GAMMA = 2.8

MIN_SPECULAR_POWER = 0.4 --wet-dry
MAX_SPECULAR_POWER = 1.5

MIN_PUDDLE_GAMMA = 0.5 --low-deep puddles
MAX_PUDDLE_GAMMA = 1.0

function WetRoadScript.onInit(self, graph)
    graph:updateProperties({name = "WetRoad"})

    -- create input linkers
    local inputs = graph:setInputLinkers{
        {
            type            = octane.PT_FLOAT,
            label           = "Wetness",
            defaultNodeType = octane.NT_FLOAT,
			bounds= {0, 1},
            defaultValue    = 0
        },
        {
            type            = octane.PT_FLOAT,
            label           = "Water Level",
            defaultNodeType = octane.NT_FLOAT,
			bounds = {0, 1},
            defaultValue    = 0
        }
    }

    m_Wetness = inputs[1]
    m_WaterLevel = inputs[2]

    -- create output linker
    local outputs = graph:setOutputLinkers{
        {
            type = octane.PT_FLOAT,
            label = "Darken",
            defaultNodeType=octane.NT_OUT_FLOAT
        },
        {
            type = octane.PT_TEXTURE,
            label = "Specular",
            defaultNodeType=octane.NT_OUT_FLOAT
        },
        {
            type = octane.PT_TEXTURE,
            label = "Smoothness",
            defaultNodeType=octane.NT_OUT_FLOAT
        },
        {
            type = octane.PT_FLOAT,
            label = "Puddle Fill",
            defaultNodeType=octane.NT_OUT_FLOAT
        },
        {
            type = octane.PT_TEXTURE,
            label = "Puddle Intensity",
            defaultNodeType=octane.NT_OUT_FLOAT
        }

    }
		
    m_Darken = octane.node.create{ type=octane.NT_FLOAT, name="darken", graphOwner=graph}
	m_Specular = octane.node.create{ type=octane.NT_TEX_FLOAT, name="specular", graphOwner=graph}
	m_Smoothness = octane.node.create{ type=octane.NT_TEX_FLOAT, name="smoothness", graphOwner=graph}
	m_PuddleFill = octane.node.create{ type=octane.NT_FLOAT, name="puddleFill", graphOwner=graph}
	m_PuddleIntensity = octane.node.create{ type=octane.NT_TEX_FLOAT, name="puddleIntensity", graphOwner=graph}
	
	outputs[1]:connectTo(octane.P_INPUT, m_Darken)
	outputs[2]:connectTo(octane.P_INPUT, m_Specular)
	outputs[3]:connectTo(octane.P_INPUT, m_Smoothness)
	outputs[4]:connectTo(octane.P_INPUT, m_PuddleFill)
	outputs[5]:connectTo(octane.P_INPUT, m_PuddleIntensity)
end

function WetRoadScript.onEvaluate(self, graph)
    --local wetness = m_Wetness:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
	local wetness = self:getInputValue(m_Wetness)  
    local wl = self:getInputValue(m_WaterLevel)  
	
	local darken = MIN_DIFFUSE_GAMMA + (MAX_DIFFUSE_GAMMA - MIN_DIFFUSE_GAMMA)*wetness --gamma of diffuse texture
    local specular = MIN_SPECULAR_POWER + (MAX_SPECULAR_POWER - MIN_SPECULAR_POWER)*wetness --power of height map inverted
    local smoothness = 1.0 - wetness/4.0 --power of roughness map
    local puddleFill = MIN_PUDDLE_GAMMA + (MAX_PUDDLE_GAMMA - MIN_PUDDLE_GAMMA)*wl -- gamma of depth of water in a puddle
    local puddleIntensity = math.max(1.0 - wl*1.5, 0)

	m_Darken:setAttribute(octane.A_VALUE, darken)
	m_Specular:setAttribute(octane.A_VALUE, specular)
	m_Smoothness:setAttribute(octane.A_VALUE, smoothness)
	m_PuddleFill:setAttribute(octane.A_VALUE, puddleFill)
	m_PuddleIntensity:setAttribute(octane.A_VALUE, puddleIntensity)

    octane.changemanager.update()
    WetRoadScript:setEvaluateTimeChanges(true)
end


return WetRoadScript