-- table with params for the "macro" node graph
params = {}
params.type = octane.GT_STANDARD
params.name = "test"
params.position = {200, 200}

local selection = octane.project.getSelection()
root = selection[1]:getProperties().graphOwner

-- creates two texture input pins and populates them with RGB texture nodes
inColor1 = octane.node.create{type=octane.NT_IN_TEXTURE, name="Color1", graphOwner=root, position={300, 30}}
inColor2 = octane.node.create{type=octane.NT_IN_TEXTURE, name="Color2", graphOwner=root, position={400, 30}}
inColor1Node = octane.node.create{type=octane.NT_TEX_RGB, pinOwnerNode=inColor1, pinOwnerId=octane.P_INPUT}
inColor2Node = octane.node.create{type=octane.NT_TEX_RGB, pinOwnerNode=inColor2, pinOwnerId=octane.P_INPUT}

-- change the color attribute for the RGB texture nodes to not 100% bright red & yellow
inColor1Node:setAttribute(octane.A_VALUE, {0.75, 0, 0})
inColor2Node:setAttribute(octane.A_VALUE, {0.75, 0.75, 0})

-- creates the output pin, material type
outMat = octane.node.create{type=octane.NT_OUT_MATERIAL, name="Material", graphOwner=root, position={450, 350}}

-- creates two materials and a material mix node
diffuse = octane.node.create{type=octane.NT_MAT_DIFFUSE, name="Diffuse", graphOwner=root, position={300, 250}}
glossy = octane.node.create{type=octane.NT_MAT_GLOSSY, name="Glossy", graphOwner=root, position={600, 250}}
matMix = octane.node.create{type=octane.NT_MAT_MIX, name="MatMix", graphOwner=root, position={450, 300}}

-- connects everything
diffuse:connectTo(octane.P_DIFFUSE, inColor1)
glossy:connectTo(octane.P_DIFFUSE, inColor2)
matMix:connectTo(octane.P_MATERIAL1, diffuse)
matMix:connectTo(octane.P_MATERIAL2, glossy)
outMat:connectTo(octane.P_INPUT, matMix)
octane.changemanager.update()