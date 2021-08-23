local RMUnityScript = {}
OCTANE_DUMMY = "RM_OctaneDummy"

function RMUnityScript.onInit(self, graph)
    graph:updateProperties({name = "RMUnity"})   

    --[[ local inputs = graph:setInputLinkers{
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
        }}

    m_Wetness = inputs[1]
    m_WaterLevel = inputs[2] ]]--

    local outputs = graph:setOutputLinkers{
        {
            type = octane.PT_FLOAT,
            label = "Wetness",
            defaultNodeType=octane.NT_OUT_FLOAT
        },
        {
            type = octane.PT_FLOAT,
            label = "Water Level",
            defaultNodeType=octane.NT_OUT_FLOAT
        }}

    m_WetnessOut = octane.node.create{ type=octane.NT_FLOAT, name="wetness", graphOwner=graph}
	m_WaterLevelOut = octane.node.create{ type=octane.NT_FLOAT, name="water level", graphOwner=graph}

    outputs[1]:connectTo(octane.P_INPUT, m_WetnessOut)
	outputs[2]:connectTo(octane.P_INPUT, m_WaterLevelOut)
end

function RMUnityScript.onEvaluate(self, graph)
    local sceneG = octane.project.getSceneGraph()
    dummyObject = sceneG:findItemsByName(OCTANE_DUMMY, true)
    local placementNode
    for i, v in pairs(dummyObject) do
        placementNode = v
        if (placementNode.type == octane.NT_GEO_PLACEMENT) then
            break
        end
    end

    local olm = placementNode.getConnectedNodeIx(placementNode, 2)
    local ol = olm.getConnectedNodeIx(olm, 2)
    local col = ol:getPinValue(octane.P_OBJECT_COLOR)

    local w = col[1]/255
    local wl = col[2]/255

    m_WetnessOut:setAttribute(octane.A_VALUE, w)
    m_WaterLevelOut:setAttribute(octane.A_VALUE, wl)

    print("RM: wetness = " .. w .."," .. " water level = " .. wl)

end

return RMUnityScript
