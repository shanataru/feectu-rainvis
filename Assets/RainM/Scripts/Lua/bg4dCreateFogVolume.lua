--
-- create an Fog cube with Lua.
--
-- This script create a node-group with a cube mesh, a specular material and a placement node
-- @author      Beppe "bepeg4d" Gullotta with the great help of Thomas "stratified" Loockx
local version = "0.2"

--GUI--

--Dimension--

-- create the x label
local noteLbl1 = octane.gui.create
{
    type    = octane.gui.componentType.LABEL,
    text    = "Cube Fog dimension:  x =",
    width   = 155,
}    
-- create a numeric field for the x Cube Fog dimension
local num1 = octane.gui.create
{
    type     = octane.gui.componentType.NUMERIC_BOX,
    minValue = 0,
    name     = "boxX",
    width    = 60,
    enable   = true,
    value    = 10,
    maxValue = 1000000
}
-- create the y label
local noteLbl2 = octane.gui.create
{
    type    = octane.gui.componentType.LABEL,
    text    = " y =",
    width   = 30,
}
-- create a numeric field for the y Cube Fog dimension
local num2 = octane.gui.create
{
    type     = octane.gui.componentType.NUMERIC_BOX,
    minValue = 0,
    name     = "boxY",
    width    = 60,
    enable   = true,
    value    = 10,
    maxValue = 1000000
} 
-- create the z label
local noteLbl3 = octane.gui.create
{
    type    = octane.gui.componentType.LABEL,
    text    = " z =",
    width   = 30,
}
-- create a numeric field for the z Cube Fog dimension
local num3 = octane.gui.create
{
    type     = octane.gui.componentType.NUMERIC_BOX,
    minValue = 0,
    name     = "boxZ",
    width    = 60,
    enable   = true,
    value    = 10,
    maxValue = 1000000
}
-- create the meter label    
local noteLbl4 = octane.gui.create
{
    type    = octane.gui.componentType.LABEL,
    text    = "meter",
    width   = 40,
}    
-- for layouting all the elements for the Fog dimension we use a group
local dimGrp = octane.gui.create
{
    type     = octane.gui.componentType.GROUP,
    text     = "Fog dimension",
    rows     = 1,
    cols     = 7,
    children =
    {
        noteLbl1, num1, noteLbl2, num2, noteLbl3, num3, noteLbl4,
    },
    padding  = { 2 },
    inset    = { 5 },
}
--Save Cube Fog--

-- create a button to show a directory chooser for saving the Cube Fog
local fileChooseButton = octane.gui.create
{
    type     = octane.gui.componentType.BUTTON,
    text     = "Choose path",
    width    = 80,
    height   = 20,
}

-- create an editor that will show the chosen file path for the Cube Fog
local fileEditor = octane.gui.create
{
    type    = octane.gui.componentType.TEXT_EDITOR,
    text    = "",
    x       = 20,
    width   = 355,
    height  = 20,
    enable  = true, 
}

-- for layouting all the elements for the cube path we use a group
local saveGrp = octane.gui.create
{
    type     = octane.gui.componentType.GROUP,
    text     = "Save the Cube Fog mesh",
    rows     = 1,
    cols     = 2,
    children =
    {
        fileChooseButton, fileEditor,
    },
    padding  = { 2 },
    inset    = { 5 },
}
-- BUTTONS --

local createButton = octane.gui.create
{
    type   = octane.gui.componentType.BUTTON,
    text   = "Create",
    width  = 120,
    height = 20,
}

local exitButton = octane.gui.create
{
    type   = octane.gui.componentType.BUTTON,
    text   = "Exit",
    width  = 120,
    height = 20,
}

local buttonGrp = octane.gui.create
{
    type     = octane.gui.componentType.GROUP,
    text     = "",
    rows     = 1,
    cols     = 2,
    children = {  createButton, exitButton},
    padding  = { 5 },
    border   = false,
}

-- group that layouts the other groups
local layoutGrp = octane.gui.create
{
    type     = octane.gui.componentType.GROUP,
    text     = "",
    rows     = 3,
    cols     = 1,
    children =
    { 
        dimGrp,
        saveGrp,
        buttonGrp,
    },
    centre   = true,
    padding  = { 2 },
    border   = false,
    debug    = false, -- true to show the outlines of the group, handy
}

-- window that holds all components
local MixMatWindow = octane.gui.create
{ 
    type     = octane.gui.componentType.WINDOW,
    text     = "Create a Cube Fog Volume v"..version,
    children = { layoutGrp },
    width    = layoutGrp:getProperties().width,
    height   = layoutGrp:getProperties().height,
}

-- END GUI CODE --

-- enable all the ui

fileChooseButton:updateProperties{ enable = true }
num1            :updateProperties{ enable = true }
createButton    :updateProperties{ enable = false}
exitButton      :updateProperties{ enable = true }

--button functions

local function createGraph()
    MixMatWindow:closeWindow(true)
end

local function exitAndDontCreateGraph()
    MixMatWindow:closeWindow(false)
end

-- global variable that holds the input path
INS_PATH = nil

-- callback handling the GUI elements
local function guiCallback(component, event)
 
    -- FILE CHOOSING --
    if component == fileChooseButton then
        -- let's save the Fog cube in a file
        local result = octane.gui.showDialog
        {
            type            = octane.gui.dialogType.FILE_DIALOG,
            title           = "save the Fog cube",
            save            = false,
            browseDirectory = true,
        }
        -- if a file is chosen
        if result.result ~= "" then
            fileEditor:updateProperties{ text = result.result }
            INS_PATH = result.result
            createButton:updateProperties{ enable = true }
        else
            createButton:updateProperties{ enable = false } 
            exitButton:updateProperties{ enable = true }
            fileEditor:updateProperties{ text = "" }
            INS_PATH = nil
        end
        print("Cube path: ", INS_PATH)

    -- close --
    elseif component == createButton then
        createGraph()
    elseif component == exitButton then
        exitAndDontCreateGraph()
        print ("ciao")
    elseif component == MixMatWindow then
        --when the window closes, print something
        if event == octane.gui.eventType.WINDOW_CLOSE then
        print (" ")
    end
end
end

-- hookup the callback with all the GUI elements
fileChooseButton:updateProperties { callback = guiCallback }
num1            :updateProperties { callback = guiCallback }
createButton    :updateProperties { callback = guiCallback }
exitButton      :updateProperties { callback = guiCallback }
MixMatWindow    :updateProperties { callback = guiCallback }

-- the script will block here until the window closes
local create = MixMatWindow:showWindow()
-- stop the script if the user clicked the exit button
if not create then return end

--Create the Cube--
params          = {}
params.type     = octane.GT_STANDARD
params.name     = "Fog"
params.position = {100, 100}

-- creates the "macro" node graph, ie. root for all other nodes
root = octane.nodegraph.create(params)


-- vertices for the cube
vertices =
{
    { -(num1.value/2) , -(num2.value/2) ,  (num3.value/2)} ,
    { -(num1.value/2) ,  (num2.value/2) ,  (num3.value/2)} ,
    {  (num1.value/2) ,  (num2.value/2) ,  (num3.value/2)} ,
    {  (num1.value/2) , -(num2.value/2) ,  (num3.value/2)} ,
    { -(num1.value/2) , -(num2.value/2) , -(num3.value/2)} ,
    { -(num1.value/2) ,  (num2.value/2) , -(num3.value/2)} ,
    {  (num1.value/2) ,  (num2.value/2) , -(num3.value/2)} ,
    {  (num1.value/2) , -(num2.value/2) , -(num3.value/2)} ,
}

-- 4 vertices per poly (each face is a quad)
verticesPerPoly =
{
    4, -- face1
    4, -- face2
    4, -- face3
    4, -- face4
    4, -- face5
    4, -- face6
}

-- tells which vertices are used for each face of the cube
-- each number is an index in vertices
-- NOTE: indices are 0-based!
polyVertexIndices =
{
    3 , 2 , 1 , 0 ,     -- face1
    0 , 1 , 5 , 4 ,     -- face2
    6 , 5 , 1 , 2 ,     -- face3
    3 , 7 , 6 , 2 ,     -- face4
    4 , 7 , 3 , 0 ,     -- face5
    4 , 5 , 6 , 7       -- face6
}

-- material names (we use the same material for each face)
materialNames = { "material" }

-- material indices (we assign 1 material per face)
polyMaterialIndices = { 0, 0, 0, 0, 0, 0 }

-- create the actual mesh node, at this point the mesh node isn't usable yet!
meshNode = octane.node.create{ type=octane.NT_GEO_MESH , name="Fog-cube", graphOwner=root, position={ 100, 100 } }
-- set up the geometry attributes. we shouldn't evaluate them immediately!
meshNode:setAttribute(octane.A_VERTICES             , vertices           , false)
meshNode:setAttribute(octane.A_VERTICES_PER_POLY    , verticesPerPoly    , false)
meshNode:setAttribute(octane.A_POLY_VERTEX_INDICES  , polyVertexIndices  , false)
meshNode:setAttribute(octane.A_MATERIAL_NAMES       , materialNames      , false)
meshNode:setAttribute(octane.A_POLY_MATERIAL_INDICES, polyMaterialIndices, false)
-- all is set up correctly, now we can evaluate the geometry
meshNode:evaluate()

meshNode:exportToFile(INS_PATH)
print("save out mesh to file: ", meshNode:getAttribute(octane.A_FILENAME))


-- get the position of the selected node in the graph editor
pos = meshNode.position

-- create a placement node and connect the mesh with it
local placeOb = octane.node.create
{
    type        =octane.NT_GEO_PLACEMENT,
    name        = "Fog-Pos",
    position    ={pos[1]-15, pos[2]+55},
    graphOwner  =root
}
placeOb:connectTo(octane.P_GEOMETRY, meshNode)

-- create a transform node and connect to the placement node
local inTansOb = octane.node.create
{
    type        = octane.NT_IN_TRANSFORM,
    name        = "Fog-PSR",
    position    = {pos[1]-75, pos[2]+25},
    graphOwner  = root
    }
placeOb:connectTo(octane.P_TRANSFORM, inTansOb)

-- create a transform node and connect to the placement node
local tansOb = octane.node.create
{
    type         = octane.NT_TRANSFORM_VALUE,
    pinOwned     = true,
    pinOwnerNode = inTansOb,
    pinOwnerId   = octane.P_INPUT,
}
-- create a geometry output node and connect to the placement node
local fogOut = octane.node.create
{
    type        =octane.NT_OUT_GEOMETRY,
    name        = "Fog-out",
    position    ={pos[1]-30, pos[2]+110},
    graphOwner  =root
}
fogOut:connectTo(octane.P_INPUT, placeOb)

-- create a in-material node and connect the mesh with it
local inMat = octane.node.create
{
    type        =octane.NT_IN_MATERIAL,
    name        = "Fog-mat",
    position    ={pos[1], pos[2]-50},
    graphOwner  =root
}

-- Connect the fog with the material pin of the mesh node
meshNode:connectToIx(1, inMat)

-- create a diffuse material pin
matNode = octane.node.create
{
    type         = octane.NT_MAT_SPECULAR,
    --name         = "Fog",
    pinOwned     = true,
    pinOwnerNode = inMat,
    pinOwnerId=octane.P_INPUT
}


-- create a blackbody node pin
sssNode = octane.node.create
{
    type         = octane.NT_MED_SCATTERING,
    name         = "SSS",
    pinOwned     = true,
    pinOwnerNode = matNode,
    pinOwnerId   = octane.P_MEDIUM,
}

-- set some value for the fog material
matNode:setPinValue(octane.P_INDEX       , 1)
matNode:setPinValue(octane.P_REFLECTION  , {0.0 , 0.0 , 0.0})
matNode:setPinValue(octane.P_FAKE_SHADOWS, true)
sssNode:setPinValue(octane.P_SCATTERING  , {0.25, 0.25, 0.25})
sssNode:setPinValue(octane.P_SCALE       , 0.05)

-- to make it a bit tidier, just unfold everything
root:unfold()