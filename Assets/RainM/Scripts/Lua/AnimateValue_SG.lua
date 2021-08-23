local AnimateValueScript = {}


local mStart
local mStop
local mSec
local mOutput


function AnimateValueScript.onInit(self, graph)
    graph:updateProperties({name = "AnimateValue"})

    -- create input linkers
    local inputs = graph:setInputLinkers
    {
        {
            type            = octane.PT_FLOAT,
            label           = "Start Value",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = {0, 0, 0},
        },
        {
            type            = octane.PT_FLOAT,
            label           = "End value",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = {0, 0, 0},
        },
        {
            type            = octane.PT_FLOAT,
            label           = "Time in seconds",
            defaultNodeType = octane.NT_FLOAT,
            defaultValue    = 10,
        }
    }
    mStart      = inputs[1]
    mStop       = inputs[2]
    mSec        = inputs[3]
        
    -- create output linker
   local outputs = graph:setOutputLinkers
        {
            {
                type = octane.PT_FLOAT,
                label = "Animated Value",
                defaultNodeType=octane.NT_OUT_FLOAT
            }
        }
    mOutput = outputs[1]
 
end


function AnimateValueScript.onEvaluate(self, graph)
    local startVal  = mStart:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
    local endVal    = mStop:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
    local totalT    = mSec:getInputNode(octane.P_INPUT):getAttribute(octane.A_VALUE)
    local Output = octane.node.create
        {
            type       = octane.NT_FLOAT,
            name       = "Ani Value",
            graphOwner = graph
        }
    Output:setAnimator(octane.A_VALUE, { 0 }, { startVal, endVal}, totalT[1])
    octane.changemanager.update()
    mOutput:connectTo(octane.P_INPUT, Output)
    AnimateValueScript:setEvaluateTimeChanges(true)
    
end


return AnimateValueScript
