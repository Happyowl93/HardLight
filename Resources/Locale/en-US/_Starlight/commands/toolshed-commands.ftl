command-description-radio-addcustom =
    Add a custom channel to the specified component on the piped entity. Specify true or false at the end to ensure the component exists.
command-description-radio-remcustom =
    Remove a custom channel with the given ID from the specified component on the piped entity.
command-description-container-insertentity =
    Inserts the given entity into the specified container on the piped entity.
command-description-container-insert =
    Inserts the piped entities into the specified container on the specified entity.
command-description-container-create =
    Creates a new container on the piped entity.
command-description-container-createslot =
    Creates a new containerslot on the piped entity.
command-description-container-delete =
    Deletes a container on the piped entity.
command-description-container-drop =
    Drops all contained entities from the specified container on the piped entity.
command-description-container-dropandget =
    Drops all contained entities from the specified container on the piped entity, and return all dropped items instead of the piped entity.
command-description-container-dropanddelete =
    Drops all contained entities from the specified container on the piped entity, then delete the container.
command-description-container-get =
    Gets the container object of the given container ID on the piped entity.
command-description-container-getentities =
    Gets all entities in the given container on the piped entity.
command-description-container-getcontaining =
    Gets all containers currently containing the piped entity.
command-description-container-getoutercontainer =
    Gets the outermost container that is containing the piped entity.
command-description-container-getowner =
    Gets the entity that owns the specified container.
command-description-solution-adjcapacity =
    Adjusts the capacity on the given solution.
command-description-solution-adjtemperature =
    Adjusts the capacity on the given solution.
command-description-solution-adjthermalenergy =
    Adjusts the capacity on the given solution.
command-description-solution-create=
    Creates a new solution with a given name on the piped entity. Returns the existing solution if it exists already.
command-description-solution-delete=
    Deletes the specified solution on the piped entity.
### Starlight (upstream #39080)
command-description-subtlemessage =
    Sends a subtle message to all the input entities.
command-description-grid-getplayers =
    Gets all players on the specified grid(s)
command-description-grid-get =
    Gets the grid(s) the specified player(s) are standing on.
command-description-grid-getstation =
    Gets the station(s) the specified player(s) are standing on.
command-description-crewmanifest-addto =
    Adds the piped entity to the specified station's crew manifest.
command-description-crewmanifest-removefrom =
    Removes the piped entity from the specified station's crew manifest.
command-description-crewmanifest-addplayer =
    Adds the specified player to the crew manifest(s) of the piped station(s).
command-description-crewmanifest-removeplayer =
    Removes the specified player to the crew manifest(s) of the piped station(s).
command-description-storage-reshape =
    Reshapes the storage based off data given via box2iconstructor command.
command-description-box2iconstructor-new =
    Create a new Box2i list definition on the entity, chain together with box2iconstructor:add commands then follow up with a command that requires it.
command-description-box2iconstructor-add =
    Add a new Box2i to the existing definition. Call box2iconstructor:new before using this.
command-description-box2iconstructor-clean =
    Clean up an unused Box2i list definition on the entity.
command-description-vector2dataconstructor-new =
    Create a new Vector2 list definition on the entity, chain together with vector2dataconstructor:add commands then follow up with a command that requires it.
command-description-vector2dataconstructor-add =
    Add a new Vector2 to the existing definition. Call vector2dataconstructor:new before using this.
command-description-vector2dataconstructor-clean =
    Clean up an unused Vector2 list definition on the entity.
command-description-job-set = 
    Changes the job of the piped entity.
command-description-clientcomp-ensure =
    Ensures that all clients add the component with the specified name to an entity, assuming it exists.
command-description-clientcomp-write =
    Attempt to make all clients vvwrite something into a client component.
command-description-clientcomp-rm =
    Ensures that all clients delete the component with the specified name from an entity, assuming it exists.
command-description-globalsound-play =
    Play a sound globally for the piped entities or sessions.
command-description-polymorph-begin =
    Marker to begin a sequence of polymorph configuration instructions, will attach a PolymorphSetupComponent to the entity.
command-description-polymorph-setproto =
    Set the prototype that the entity will polymorph into.
command-description-polymorph-seteffect =
    Set a prototype to spawn on top of the polymorphed entity, typically this is used to create special effects.
command-description-polymorph-setdelay =
    Set how long in seconds must be waited before being able to activate this specific polymorph again.
command-description-polymorph-setduration =
    Set the duration the polymorph should last for in seconds before automatically reverting.
command-description-polymorph-setforced =
    Set to make so the polymorph cannot be activated or canceled by the entity itself.
command-description-polymorph-settransferdamage =
    Set to transfer the damage from the current entity to the polymorphed entity.
command-description-polymorph-settransfername =
    Set to make the polymorphed entity inherit the name of the original.
command-description-polymorph-settransferappearance =
    Set whether to transfer things like hair, skin color, height, etc. to the polymorphed entity.
command-description-polymorph-setinventory =
    Set to determine how the entity's inventory will transfer to the polymorphed entity.
command-description-polymorph-setrevertoncrit =
    Set whether to revert the polymorph when the entity enters a critical state or not.
command-description-polymorph-setrevertondeath =
    Set whether to revert the polymorph when the entity is killed or not.
command-description-polymorph-setrevertondelete =
    Set whether to revert the polymorph when the entity is deleted or not.
command-description-polymorph-setrevertoneat =
    Set whether to revert the polymorph when the entity is eaten or not.
command-description-polymorph-setallowrepeats =
    Set whether to allow repeated polymorphs or not.
command-description-polymorph-setignoreallowrepeats =
    Set to allow the polymorph to happen even if AllowRepeatedMorphs is true.
command-description-polymorph-setcooldown =
    Set the cooldown in seconds before another polymorph can take place.
command-description-polymorph-setentersound =
    Set the sound that plays when entering the polymorph.
command-description-polymorph-setexitsound =
    Set the sound that plays when exiting the polymorph.
command-description-polymorph-clearentersound =
    Clear the sound that plays when entering the polymorph.
command-description-polymorph-clearexitsound =
    Clear the sound that plays when exiting the polymorph.
command-description-polymorph-setenterpopup =
    Set the popup that appears when entering the polymorph.
command-description-polymorph-setexitpopup =
    Set the popup that appears when exiting the polymorph.
command-description-polymorph-clearcopycomp =
    Clear the list of components to copy to the polymorph.
command-description-polymorph-addcopycomp =
    Add an entry to the list of components to copy to the polymorph.
command-description-polymorph-rmcopycomp =
    Remove an entry from the list of components to copy to the polymorph.
command-description-polymorph-apply =
    Instantly apply the polymorph and finish.
command-description-polymorph-addaction =
    Add a polymorph action to the entity using the current polymorph setup chain. You should probably call polymorph:apply or polymorph:finish afterward.
command-description-polymorph-addactionproto =
    Add a prototyped polymorph action to the entity.
command-description-polymorph-rmaction =
    Remove a polymorph action from the entity that was added via polymorph:addaction.
command-description-polymorph-rmactionproto =
    Remove a prototyped polymorph action from the entity.
command-description-polymorph-revert =
    Revert to the previous x entity, if possible.
command-description-polymorph-reset =
    Reset the entity's polymorph to their original state.
command-description-polymorph-finish =
    Marks this polymorph setup chain as complete, cleaning up and removing the component.