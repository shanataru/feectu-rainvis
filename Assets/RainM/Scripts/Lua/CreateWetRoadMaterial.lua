--[[
Before running this script on a PBR Override material, have a "Material out" node selected
]]

local CreateWetRoadMaterial = {}

local m_waveScale = 2.0
local m_scriptDir
local m_textureDir


function CreateWetRoadMaterial.onInit(self, graph)
    graph:updateProperties({name = "CreateWetRoadMaterial"})

    -- create input linkers
    local inputs = graph:setInputLinkers{
        {
            type            = octane.PT_STRING,
            label           = "ScriptDir",
            defaultNodeType = octane.NT_DIRECTORY,
            defaultValue    = ""
        },
        --[[
        {
        	type            = octane.PT_STRING,
            label           = "TextureDir",
            defaultNodeType = octane.NT_DIRECTORY,
            defaultValue    = ""
        }
		]]
    }

    m_scriptDir = inputs[1]
    --m_textureDir = inputs[2]
end

function CreateWetRoadMaterial.onEvaluate(self, graph)
    local selection = octane.project.getSelection()
    local go = selection[1]:getProperties().graphOwner
    local sdir = self:getInputValue(m_scriptDir) 
    if(sdir == "") then 
        return
    end
    --local tdir = self:getInputValue(m_textureDir)
    local matOut = go:findFirstNode(octane.NT_OUT_MATERIAL)

    -- creates texture inputs
    inAO = octane.node.create{type=octane.NT_TEX_IMAGE, name="AmbientOcclusion", graphOwner=go, position={0, 0}}
    inAlbedo = octane.node.create{type=octane.NT_TEX_IMAGE, name="Albedo", graphOwner=go, position={200, 0}}
    inHeight = octane.node.create{type=octane.NT_TEX_IMAGE, name="Height", graphOwner=go, position={400, 0}}
    inNormal = octane.node.create{type=octane.NT_TEX_IMAGE, name="Normal", graphOwner=go, position={600, 0}}
    inRoughness = octane.node.create{type=octane.NT_TEX_IMAGE, name="Roughness", graphOwner=go, position={800, 0}}
    inPuddleMap = octane.node.create{type=octane.NT_TEX_IMAGE, name="PuddleMap", graphOwner=go, position={1000, 0}}
    inWaterNormal1 = octane.node.create{type=octane.NT_TEX_IMAGE, name="Wave Normal 1", graphOwner=go, position={1200, 0}}
    inWaterNormal2 = octane.node.create{type=octane.NT_TEX_IMAGE, name="Wave Normal 2", graphOwner=go, position={1400, 0}}

    --load texture files into nodes
    --inAO:setAttribute(octane.A_FILENAME,)

    mixTexWaterNormal = octane.node.create{type=octane.NT_TEX_MIX, name="Mix texture", graphOwner=go, position={1300, 50}}
    multiplyAlbedoAO = octane.node.create{type=octane.NT_TEX_MULTIPLY, name="Multiply texture", graphOwner=go, position={0, 50}}

    --create color correction nodes
    colCorrMultAlbedo = octane.node.create{type=octane.NT_TEX_COLORCORRECTION, name="Color correction", graphOwner=go, position={200, 100}}
    colCorrAlbedo = octane.node.create{type=octane.NT_TEX_COLORCORRECTION, name="Color correction", graphOwner=go, position={400, 100}}
    colCorrHeight1 = octane.node.create{type=octane.NT_TEX_COLORCORRECTION, name="Color correction", graphOwner=go, position={600, 100}}
    colCorrHeight2 = octane.node.create{type=octane.NT_TEX_COLORCORRECTION, name="Color correction", graphOwner=go, position={800, 100}}

    oslProjPuddles = octane.node.create{type=octane.NT_PROJ_OSL, name="OSL Puddles", graphOwner=go, position={1000, -50}}
    oslProjWave1 = octane.node.create{type=octane.NT_PROJ_OSL, name="OSL Waves", graphOwner=go, position={1200, -50}}
    oslProjWave2 = octane.node.create{type=octane.NT_PROJ_OSL, name="OSL Waves", graphOwner=go, position={1400, -50}}
    oslTex = octane.node.create{type=octane.NT_TEX_OSL, name="OSL Tex Screen", graphOwner=go, position={1000, 200}}
    scriptedGraphWetRoad = octane.nodegraph.create{type=octane.GT_SCRIPTED, name="SG Wet Road", graphOwner=go, position={700, -200}}

	-- load script files into nodes
    oslProjPuddles:setAttribute(octane.A_FILENAME, sdir .. "\\OSL\\OslPuddles.osl")
    oslProjWave1:setAttribute(octane.A_FILENAME, sdir .. "\\OSL\\OslWaves1.osl")
    oslProjWave2:setAttribute(octane.A_FILENAME, sdir .. "\\OSL\\OslWaves2.osl")
    oslTex:setAttribute(octane.A_FILENAME, sdir .. "\\OSL\\OslScreenBlend.osl")
    scriptedGraphWetRoad:setAttribute(octane.A_FILENAME, sdir .. "\\Lua\\WetRoad.lua")

    --set pin values where necessary
    colCorrHeight1:setPinValue(octane.P_INVERT, true)
    colCorrHeight1:setPinValue(octane.P_CONTRAST, 0.0)
    colCorrHeight2:setPinValue(octane.P_CONTRAST, 5.0)
    colCorrHeight2:setPinValue(octane.P_BRIGHTNESS, 1.2)
    colCorrAlbedo:setPinValue(octane.P_GAMMA, 2.2)
    oslProjWave1:setPinValueIx(2, m_waveScale)
    oslProjWave2:setPinValueIx(2, m_waveScale)


    --create final material
    addTex = octane.node.create{type=octane.NT_TEX_ADD, name="Add texture", graphOwner=go, position={400, 300}}
    matRoad = octane.node.create{type=octane.NT_MAT_GLOSSY, name="Road Material", graphOwner=go, position={700, 300}}
    matPuddle = octane.node.create{type=octane.NT_MAT_GLOSSY, name="Puddle Material", graphOwner=go, position={1100, 300}}
    matMix = octane.node.create{type=octane.NT_MAT_MIX, name="Mix Material", graphOwner=go, position={800, 400}}

    --connect nodes
    matMix:connectTo(octane.P_MATERIAL1, matRoad)
    matMix:connectTo(octane.P_MATERIAL2, matPuddle)
    multiplyAlbedoAO:connectTo(octane.P_TEXTURE1, inAlbedo)
    multiplyAlbedoAO:connectTo(octane.P_TEXTURE2, inAO)
    colCorrMultAlbedo:connectTo(octane.P_TEXTURE, multiplyAlbedoAO)
    colCorrAlbedo:connectTo(octane.P_TEXTURE, inAlbedo)
    colCorrHeight1:connectTo(octane.P_TEXTURE, inHeight)
    colCorrHeight2:connectTo(octane.P_TEXTURE, inHeight)
    inPuddleMap:connectTo(octane.P_PROJECTION, oslProjPuddles)
    inWaterNormal1:connectTo(octane.P_PROJECTION, oslProjWave1)
    inWaterNormal2:connectTo(octane.P_PROJECTION, oslProjWave2)
    mixTexWaterNormal:connectTo(octane.P_TEXTURE1, inWaterNormal1)
    mixTexWaterNormal:connectTo(octane.P_TEXTURE2, inWaterNormal2)
    matPuddle:connectTo(octane.P_NORMAL, mixTexWaterNormal)
    matPuddle:connectTo(octane.P_BUMP, oslTex)
    matPuddle:connectTo(octane.P_DIFFUSE, colCorrAlbedo)
    matRoad:connectTo(octane.P_NORMAL, inNormal)
    matRoad:connectTo(octane.P_ROUGHNESS, inRoughness)
    matRoad:connectTo(octane.P_SPECULAR, colCorrHeight1)
    matRoad:connectTo(octane.P_DIFFUSE, colCorrMultAlbedo)
    matMix:connectTo(octane.P_AMOUNT, addTex)
    matOut:connectTo(octane.P_INPUT, matMix)

    pinsWetRoad = scriptedGraphWetRoad:getOutputNodes()
    colCorrMultAlbedo:connectTo(octane.P_GAMMA, pinsWetRoad[1])
    colCorrHeight1:connectTo(octane.P_BRIGHTNESS, pinsWetRoad[2])
    inRoughness:connectTo(octane.P_POWER, pinsWetRoad[3])
    colCorrHeight2:connectTo(octane.P_GAMMA, pinsWetRoad[4])
    addTex:connectTo(octane.P_TEXTURE1, pinsWetRoad[5])
    addTex:connectTo(octane.P_TEXTURE2, oslTex)
    oslTex:connectToIx(1, inPuddleMap)
    oslTex:connectToIx(2, colCorrHeight2)
end

return CreateWetRoadMaterial