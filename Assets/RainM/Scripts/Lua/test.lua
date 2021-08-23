someTable = {}                                    -- create an empty table
someTable.type = octane.gui.componentType.SLIDER  -- you will find all these properties in the docs in the PROPS_GUI_SLIDER item.
                                                  -- the type takes one of the constants in octane.gui.componentType.
someTable.minValue = 0
someTable.maxValue = 10
someTable.step = 2
someTable.value = 6
slider = octane.gui.create(someTable)