action-vampire-toggle-fangs-name = Toggle Fangs
action-vampire-toggle-fangs-desc = Extend or retract your fangs to drink blood from victims.

action-vampire-glare-name = Glare
action-vampire-glare-desc = Paralyze and mute nearby targets, dealing stamina damage over time.

alerts-vampire-blood-name = Blood Drunk
alerts-vampire-blood-desc = Shows how much blood you've drunk. Extend your fangs and left-click a target to drink.

alerts-vampire-fed-name = Blood Fullness
alerts-vampire-fed-desc = Your current blood fullness. Drink blood to stay fed.

roles-antag-vamire-name = Vampire
roles-antag-vampire-description = Feed on the crew. Extend your fangs and drink their blood.

vampire-drink-start = You sink your fangs into {CAPITALIZE(THE($target))}.
vampire-drink-end = You drink blood from {CAPITALIZE(THE($target))}.

action-vampire-rejuvenate-i-name = Rejuvenate
action-vampire-rejuvenate-i-desc = Instantly remove stuns and recover 100 stamina damage.

action-vampire-rejuvenate-ii-name = Rejuvenate
action-vampire-rejuvenate-ii-desc = Instantly remove stuns and recover 100 stamina, plus purge harmful reagents (10u) and heal 10 brute, 10 burn, 10 toxin, and 50 oxy loss.

action-vampire-class-select-name = Choose Vampire Class

vampire-not-enough-blood = Not enough blood.

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
    Focuses on blood magic and the manipulation of blood around you.
    

vampire-class-umbrae-tooltip = Umbrae
    Focuses on darkness, stealth ambushing and mobility.

vampire-class-gargantua-tooltip = Gargantua
    Focuses on tenacity and melee damage.


vampire-class-dantalion-tooltip = Dantalion
    Focuses on thralling and illusions.

# Hemomancer abilities
action-vampire-hemomancer-claws-name = Vampiric Claws()
action-vampire-hemomancer-claws-desc = Extend blood-infused claws, increasing your melee damage significantly.

action-vampire-hemomancer-tendrils-name = Blood Tendrils()
action-vampire-hemomancer-tendrils-desc = Send blood tendrils to a target location, slowing and poisoning enemies.

action-vampire-blood-barrier-name = Blood Barrier()
action-vampire-blood-barrier-desc = Creates 3 blood barriers at the target location. Vampires can pass through them.

action-vampire-sanguine-pool-name = Sanguine Pool()
action-vampire-sanguine-pool-desc = Briefly transform into a sentient blood puddle, increasing movement speed and allowing you to move through anything except walls and space.

action-vampire-blood-eruption-name = Blood Eruption()
action-vampire-blood-eruption-desc = Cause any blood within 4 tiles of you to erupt, dealing 50 brute damage to anyone standing on it.

action-vampire-blood-bringers-rite-name = Blood Bringers Rite(10/2 sec)
action-vampire-blood-bringers-rite-desc = todo

# Umbrae abilities
action-vampire-cloak-of-darkness-name = Cloak of Darkness(Toggle)
action-vampire-cloak-of-darkness-desc = Toggle invisibility and speed boost that scales with darkness. More effective in dark areas, less effective in bright light.

action-vampire-shadow-snare-name = Shadow Snare(20/10)
action-vampire-shadow-snare-desc = Place a fragile shadow trap at target location. Damages, blinds (20s) and heavily slows a non-vampire humanoid that steps on it. Trap health decays in brighter light and is destroyed by flashes.
action-vampire-shadow-anchor-name = Shadow Anchor(20/10)
action-vampire-shadow-anchor-desc = First use: place a shadow anchor beacon (lasts 2 min). Second use while it exists: instantly return to it and consume the beacon.
