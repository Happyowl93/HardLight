action-vampire-toggle-fangs-name = Toggle Fangs
action-vampire-toggle-fangs-desc = Extend or retract your fangs to drink blood from victims.

action-vampire-glare-name = Glare(Free)
action-vampire-glare-desc = Paralyze and mute nearby targets, dealing stamina damage over time.

alerts-vampire-blood-name = Blood Drunk
alerts-vampire-blood-desc = Shows how much blood you've drunk. Extend your fangs and left-click a target to drink.

alerts-vampire-fed-name = Blood Fullness
alerts-vampire-fed-desc = Your current blood fullness. Drink blood to stay fed.

alerts-vampire-blood-swell-name = Blood Swell
alerts-vampire-blood-swell-desc = Blood Swell is empowering you.

alerts-vampire-blood-rush-name = Blood Rush
alerts-vampire-blood-rush-desc = Blood Rush is speeding you up.

vampire-blood-swell-cancel-shoot = Your fingers don`t fit in the trigger guard!!

roles-antag-vamire-name = Vampire
roles-antag-vampire-description = Feed on the crew. Extend your fangs and drink their blood.

vampire-drink-start = You sink your fangs into {CAPITALIZE(THE($target))}.

action-vampire-rejuvenate-i-name = Rejuvenate(Free)
action-vampire-rejuvenate-i-desc = Instantly remove stuns and recover 100 stamina damage.

action-vampire-rejuvenate-ii-name = Rejuvenate(Free)
action-vampire-rejuvenate-ii-desc = Instantly remove stuns and recover 100 stamina, plus purge harmful reagents (10u) and heal 10 brute, 10 burn, 10 toxin, and 50 oxy loss.

action-vampire-class-select-name = Choose Vampire Class

vampire-not-enough-blood = Not enough blood.

vampire-mouth-covered = Your mouth is covered!
vampire-drink-target-maxed = You have already drunk { $amount } units of blood from this target.
vampire-drink-target-hard-max = You have drunk the maximum amount of blood from this target ({ $amount } units).
vampire-full-power-achieved = Your vampiric essence surges full power achieved!

vampire-role-greeting = You are a vampire!
    Your blood thirst compels you to feed on crew members. Use your abilities to turn other crew.
    Your fangs allow you to suck blood from humans. Blood will regenerate health and give you new abilities.
    Find something to accomplish during this shift!

# Objectives
objective-condition-drain-title = Drain {$count} units of blood
objective-condition-drain-description = Drink {$count} units of blood from crew members using your fangs.

# Class selection action
action-vampire-class-select = Select vampire class
action-vampire-class-select-desc = Choose your vampire subclass

# Round end statistics
roundend-prepend-vampire-drained-low = The vampires barely fed this shift, draining only {$blood} units of blood.
roundend-prepend-vampire-drained-medium = The vampires had a decent meal, draining {$blood} units of blood.
roundend-prepend-vampire-drained-high = The vampires had a blood feast, draining {$blood} units of blood!
roundend-prepend-vampire-drained-critical = The vampires went on a feeding frenzy, draining a staggering {$blood} units of blood!

roundend-prepend-vampire-drained = No vampires managed to drain any significant amount of blood this round.
roundend-prepend-vampire-drained-named = {$name} was the most bloodthirsty vampire, draining {$number} units of blood total.

# Vampire class selection tooltips
vampire-class-hemomancer-tooltip = Hemomancer
    Focuses on blood magic and the manipulation of blood around you
    

vampire-class-umbrae-tooltip = Umbrae
    Focuses on darkness, stealth ambushing and mobility

vampire-class-gargantua-tooltip = Gargantua
    Focuses on tenacity and melee damage


vampire-class-dantalion-tooltip = Dantalion
    Focuses on thralling and illusions

# Hemomancer abilities
action-vampire-hemomancer-claws-name = Vampiric Claws()
action-vampire-hemomancer-claws-desc = Extend blood-infused claws, increasing your melee damage significantly.

action-vampire-hemomancer-tendrils-name = Blood Tendrils()
action-vampire-hemomancer-tendrils-desc = Send blood tendrils to a target location, slowing and poisoning enemies.
action-vampire-hemomancer-tendrils-wrong-place = Cannot cast there.

action-vampire-blood-barrier-name = Blood Barrier()
action-vampire-blood-barrier-desc = Creates 3 blood barriers at the target location. Vampires can pass through them.
action-vampire-blood-barrier-wrong-place = Cannot place barriers there.

action-vampire-sanguine-pool-name = Sanguine Pool()
action-vampire-sanguine-pool-desc = Briefly transform into a sentient blood puddle, increasing movement speed and allowing you to move through anything except walls and space.
action-vampire-sanguine-pool-already-in = You are already in sanguine pool form!
action-vampire-sanguine-pool-invalid-tile = You cannot become a blood pool here.
action-vampire-sanguine-pool-enter = You transform into a pool of blood!
action-vampire-sanguine-pool-exit = You reform from the blood pool!
vampire-space-burn-warning = The harsh void light scorches your undead flesh!

action-vampire-blood-eruption-name = Blood Eruption()
action-vampire-blood-eruption-desc = Cause any blood within 4 tiles of you to erupt, dealing 50 brute damage to anyone standing on it.
action-vampire-blood-eruption-activated = You cause blood to erupt in spikes around you!

action-vampire-blood-bringers-rite-name = Blood Bringers Rite(10/2 sec)
action-vampire-blood-bringers-rite-desc = todo
action-vampire-blood-bringers-rite-not-enough-power = You lack full vampiric power (need above 1000 total blood & 8 unique victims)
action-vampire-blood-brighters-rite-not-enough-blood = Not enough blood to activate blood bringers rite
action-vampire-blood-bringers-rite-start = Blood Bringers Rite activated!
action-vampire-blood-bringers-rite-stop = Blood bringers rite deactivated
action-vampire-blood-bringers-rite-stop-blood = Blood Bringers Rite deactivated - not enough blood

# Umbrae abilities
action-vampire-cloak-of-darkness-name = Cloak of Darkness(Toggle)
action-vampire-cloak-of-darkness-desc = Toggle invisibility and speed boost that scales with darkness. More effective in dark areas, less effective in bright light.
action-vampire-cloak-of-darkness-start = You blend into the shadows!
action-vampire-cloak-of-darkness-stop = You step out of the shadows.


action-vampire-shadow-snare-name = Shadow Snare(20/10)
action-vampire-shadow-snare-desc = Place a fragile shadow trap at target location. Damages, blinds (20s) and heavily slows a non-vampire humanoid that steps on it. Trap health decays in brighter light and is destroyed by flashes.
action-vampire-shadow-snare-scatter = You scattered the shadow trap.
vampire-shadow-snare-oldest-removed = Your old shadow snare dissipates.
ent-shadow-snare-ensnare = shadow snare

action-vampire-shadow-anchor-name = Shadow Anchor(20/10)
action-vampire-shadow-anchor-desc = First use: place a shadow anchor beacon (lasts 2 min). Second use while it exists: instantly return to it and consume the beacon.
action-vampire-shadow-anchor-returned = You returned to the shadow anchor
action-vampire-shadow-anchor-installed = You've secured a spot in the shadows


action-vampire-shadow-boxing-start = You begin shadow boxing.
action-vampire-shadow-boxing-stop = Shadow boxing has been stoped.
action-vampire-shadow-boxing-ends = Shadow boxing ends.

action-vampire-dark-passage-wrong-place = The darkness here is impenetrable...
action-vampire-dark-passage-activated = You slipped through the darkness...

action-vampire-extinguish-activated = You absorbed the light around you...({$count})

action-vampire-eternal-darkness-not-enough-power = Your power is insufficient (need >1000 total blood & 8 unique victims).
action-vampire-eternal-darkness-not-enough-blood = You have run out of blood to sustain eternal darkness.
action-vampire-eternal-darkness-start = You conjured eternal darkness...
action-vampire-eternal-darkness-stop = The eternal darkness has dissipated...

#Dantalion
action-vampire-enthrall-name = Enthrall(150)
action-vampire-enthrall-desc = Channel for 15 seconds on a humanoid target to bind them to your will. Cancels if either of you moves.
vampire-enthrall-start = You reach into {CAPITALIZE(THE($target))}'s mind...
vampire-enthrall-success = {CAPITALIZE(THE($target))} bends the knee and becomes your thrall.
vampire-enthrall-target = Your mind is overwhelmed by vampiric domination!
vampire-enthrall-limit = You cannot control any more thralls.
vampire-enthrall-invalid = That target cannot be enthralled.
vampire-thrall-released = The vampiric hold over you fades.

action-vampire-pacify-name = Pacify(10)
action-vampire-pacify-desc = Flood a victim's mind with bliss, pacifying them for 40 seconds.
vampire-pacify-invalid = That target cannot be pacified.
vampire-pacify-success = {CAPITALIZE(THE($target))} succumbs to your overwhelming serenity.
vampire-pacify-target = A crushing calm drowns your will to fight!

action-vampire-subspace-swap-name = Subspace Swap(30)
action-vampire-subspace-swap-desc = Select a target within 7 tiles to swap positions, slowing them for 4 seconds.
vampire-subspace-swap-thrall = You cannot subspace swap with your thralls.
vampire-subspace-swap-dead = That mind is beyond your reach.
vampire-subspace-swap-failed = The subspace rift fizzles uselessly.
vampire-subspace-swap-success = Space twists as you trade places with {CAPITALIZE(THE($target))}!
vampire-subspace-swap-target = Reality warps and you are torn into a new position!

action-vampire-decoy-name = Decoy(30)
action-vampire-decoy-desc = Leave behind a fragile duplicate that blinds attackers when harmed while you vanish into invisibility.

action-vampire-rally-thralls-name = Rally Thralls(100)
action-vampire-rally-thralls-desc = Command thralls within 7 tiles to shake off stuns, wake up, and regain stamina.
vampire-rally-thralls-success = {$count ->
    [one] Your call rallies a thrall back to your side!
    *[other] Your call rallies {$count} thralls back to your side!
}
vampire-rally-thralls-none = None of your thralls can answer the call.

action-vampire-blood-bond-name = Blood Bond(Toggle)
action-vampire-blood-bond-desc = Toggle a blood tether to nearby thralls, redistributing damage between you at the cost of 2.5 blood per second.
vampire-blood-bond-start = Rivers of blood knit you to your thralls.
vampire-blood-bond-stop = You let the blood bond fall slack.
vampire-blood-bond-no-thralls = You have no enthralled servants to bond with.
vampire-blood-bond-stop-blood = The bond shreds itself; you lack the blood to sustain it.

action-vampire-mass-hysteria-name = Mass Hysteria(70)
action-vampire-mass-hysteria-desc = Flood every nearby mind (except thralls) with terror, flashing them and cursing them with hallucinations for 30 seconds.

# Gargantua abilities
action-vampire-blood-swell-name = Blood Swell(30)
action-vampire-blood-swell-desc = For 30 seconds: reduces brute damage by 60%, stamina and burn by 50%, halves stun times. Cannot use guns. After 400 blood: also gain +14 melee damage.
vampire-blood-swell-start = Your muscles swell with unholy power!
vampire-blood-swell-end = The blood rage subsides.

action-vampire-blood-rush-name = Blood Rush(30)
action-vampire-blood-rush-desc = For 10 seconds: double your movement speed.
vampire-blood-rush-start = Blood surges through your limbs!
vampire-blood-rush-end = Your supernatural speed fades.

action-vampire-seismic-stomp-name = Seismic Stomp(30)
action-vampire-seismic-stomp-desc = Slam the ground, knocking down and throwing all creatures within 3 tiles away from you. Destroys floor tiles.
vampire-seismic-stomp-activate = The ground shudders beneath your fury!

action-vampire-overwhelming-force-name = Overwhelming Force(Toggle)
action-vampire-overwhelming-force-desc = Toggle: automatically pry open unpowered doors. While active, you cannot be pushed or pulled. Costs 5 blood per door.
vampire-overwhelming-force-start = Your presence becomes immovable.
vampire-overwhelming-force-stop = You relax your iron grip.
vampire-overwhelming-force-too-heavy = This object is far too heavy to move!
vampire-overwhelming-force-door-pried = You wrench the door open with brute strength.

action-vampire-demonic-grasp-name = Demonic Grasp(20)
action-vampire-demonic-grasp-desc = Launch a demonic hand up to 15 tiles. Immobilizes the target for 5 seconds. In combat mode, also pulls them to you.
vampire-demonic-grasp-cast = You launch a demonic claw!
vampire-demonic-grasp-hit = A demonic claw seizes you!
vampire-demonic-grasp-pull = The claw drags you toward the vampire!

action-vampire-charge-name = Charge(30)
action-vampire-charge-desc = Charge in the target direction until hitting an obstacle or void. Creatures take 60 brute and are thrown 5 tiles. Structures take 150 damage. Walls are destroyed.
vampire-charge-start = You barrel forward with unstoppable force!
vampire-charge-impact = You crash into {CAPITALIZE(THE($target))} with devastating force!
