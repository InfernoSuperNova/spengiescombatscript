using EmptyKeys.UserInterface.Generated.DataTemplatesContracts_Bindings;
using IngameScript.Classes;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using VRageRender.Messages;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        
        //Inconspcuous name :D
        string GroupName = "Flight Control";

        float ProjectileVelocity = 2000;                //Initialize this if you only have one primary weapon, otherwise run with argument to set velocity
        float ProjectileMaxDist = 2000;                 //Maximum distance to target, used for determining if the ship should fire or not, and if it should approach the target or not
        float TurretVelocity = 500.0f;                  //velocity that the turrets use for turret aim overriding
        float rollSensitivityMultiplier = 1;            //Increase if spinning too slowly, decrease if spi   nning too quickly
        float maxAngular = 30.0f;                       //Max angular velocity in RPM, set to 60 for small gridId and 30 for large gridId (or something less if you wish for slower seek)
        bool predictAcceleration = true;
        float framesToGroupGuns = 5;                    //Frame leeway to consider guns as part of the same volley
        AimType aimType = AimType.CenterOfMass;         //Valid options are CenterOfMass, TurretAverage, and RandomTurretTarget. Can also be set with argument


        /// COMPLETELY AUTONOMOUS MODE ///
        bool AutonomousMode = true;                     // complete control over the ship
        float autonomousDesiredDistance = 1500;         //Distance to idle at
        
        double minAutonomousRollPower = 0.5;            //minimum roll power
        double maxAutonomousRollPower = 2.2;            //maximum roll power
        double autonomousRollChangeFrames = 150;        //frames to wait before changing roll power
        double maxRollPowerToFlipRollSign = 0.7;        //maximum speed above which the ship cannot roll the dice to change the roll sign
        float probabilityOfFlippingRollSign = 0.7f;     //probability of flipping the roll sign (assuming the ship is below the max speed to switch roll sign)
        float autonomousFireSigma = 0.9997f;            //how close to on target the ship needs to be to fire

        bool SendEnemyLocation = true;                  //Send enemy location to other ships
        bool ReceiveEnemyLocation = true;               //Receive enemy location from other ships (autonomous required)
        string TaskForceTag = "TaskForceOne";           //Tag use for co ordination between ships, use a different tag if you want co ordination with a different group

        float FriendlyAvoidanceThreshold = 10;          //Distance to stop moving away from friendlies
        float minimumGridDimensions = 100;               //minimum grid dimensions for the ship to be targeted
        //Used for maintaining distance
        float autonomouskP = 0.5f;
        float autonomouskI = 0.0f;
        float autonomouskD = 1f;

        float flightDeck = 100;                         //Distance below which to try and go back up if in gravity
        int maxFramesToFollowDriftingTarget = 600;

        /// PID CONFIG ///
        double kP = 40;
        double kI = 0;
        double kD = 25;
        double derivativeNonLinearity = 2;
        double integralClamp = 0.05;

        const double TimeStep = 1.0 / 60.0;

        //offset the forward reference
        float OffsetVert = -5;                          //offset in meters, positive is up
        float OffsetCoax = 0;                           //offset in meters, positive is forward
        float OffsetHoriz = 0;                          //offset in meters, positive is right

        float PassiveRadius = 300;                      //For passive antenna range
        float TransmitRadius = 50000;                   //For transmitting enemy location
        string TransmitMessage = "";
        


        

        //jump drive config, requires the jump drive API mod (and autonomous mode)
        bool useJumping = true;
        float minDistanceToJump = 10000;


        bool UseRandomTransmitMessage = true;
        int framesPerTransmitMessage = 1200;
        int transmitMessageHoldTimeFrames = 300;
        List<string> randomQuotes = new List<string>()
        {
                        // Random quotes
            "I'm sorry, Dave. I'm afraid I can't do that.",
            "Do you know who ate all the doughnuts?",
            "Sometimes I dream about cheese.",
            "Why do we all have to wear these ridiculous ties?",
        };
        List<string> gladosQuotes = new List<string>()
        {
                        // Portal quotes
            "Hello and, again, welcome to the Aperture Science computer-aided enrichment center.",
            "We hope your brief detention in the relaxation vault has been a pleasant one.",
            "Your specimen has been processed and we are now ready to begin the test proper.",
            "Before we start, however, keep in mind that although fun and learning are the primary goals of all enrichment center activities, serious injuries may occur.",
            "For your own safety and the safety of others, please refrain from touching [bzzzzzt]",
            "Por favor bordón de fallar Muchos gracias de fallar gracias",
            "Stand back. The portal will open in 3... 2... 1...",
            "Excellent. Please proceed into the chamberlock after completing each test.",
            "First, however, note the incandescent particle field across the exit. This Aperture Science Material Emancipation Grid will vaporize any unauthorized equipment that passes through it.",
            "Please place the Weighted Storage Cube on the Fifteen Hundred Megawatt Aperture Science Heavy Duty Super-Colliding Super Button.",
            "Perfect. Please move quickly to the chamberlock, as the effects of prolonged exposure to the Button are not part of this test.",
            "You're doing very well!",
            "Please be advised that a noticeable taste of blood is not part of any test protocol but is an unintended side effect of the Aperture Science Material Emancipation Grill, which may, in semi-rare cases, emancipate dental fillings, crowns, tooth enamel, and teeth.",
            "Very good! You are now in possession of the Aperture Science Handheld Portal Device.",
            "With it, you can create your own portals. These intra-dimensional gates have proven to be completely safe.",
            "The device, however, has not. Do not touch the operational end of the device.",
            "Do not look directly at the operational end of the device.",
            "Do not submerge the device in liquid, even partially. Most importantly, under no circumstances should you [bzzzpt]",
            "Please proceed to the chamberlock. Mind the gap.",
            "Well done. Remember, the Aperture Science Bring Your Daughter to Work Day is the perfect time to have her tested.",
            "Welcome to test chamber four. You're doing quite well.",
            "Once again, excellent work. As part of a required test protocol, we will not monitor the next test chamber. You will be entirely on your own. Good luck.",
            "To ensure the safe performance of all authorized activities, do not destroy vital testing apparatus.",
            "As part of a required test protocol, our previous statement suggesting that we would not monitor this chamber was an outright fabrication.",
            "Good job! As part of a required test protocol, we will stop enhancing the truth in three... two...",
            "While safety is one of many Enrichment Center goals, the Aperture Science High Energy Pellet, seen to the left of the chamber, can and has caused permanent disabilities such as vaporization.",
            "Please be careful.",
            "Unbelievable! You, Subject Name Here, must be the pride of Subject Hometown Here.",
            "Warning devices are required on all mobile equipment. However, alarms and flashing hazard lights have been found to agitate the high energy pellet and have therefore been disabled for your safety.",
            "Good. Now use the Aperture Science Unstationary Scaffold to reach the chamberlock.",
            "Please note that we have added a consequence for failure. Any contact with the chamber floor will result in an unsatisfactory mark on your official testing record, followed by death. Good luck!",
            "Very impressive. Please note that any appearance of danger is merely a device to enhance your testing experience.",
            "The Enrichment Center regrets to inform you that this next test is impossible. Make no attempt to solve it.",
            "The enrichment center apologizes for this clearly broken test chamber.",
            "Once again, the Enrichment Center offers its most sincere apologies on the occasion of this unsolvable test environment.",
            "Frankly, this chamber was a mistake. If we were you, we would quit now.",
            "No one will blame you for giving up. In fact, quitting at this point is a perfectly reasonable response.",
            "Quit now and cake will be served immediately.",
            "Fantastic. You remained resolute and resourceful in an atmosphere of extreme pessimism.",
            "Hello again. To reiterate our previous warning, this test [garbled] momentum [garbled]",
            "Spectacular. You appear to understand how a portal affects forward momentum, or to be more precise, how it does not.",
            "Momentum, a function of mass and velocity, is conserved between portals. In layman's terms: speedy thing goes in, speedy thing comes out.",
            "The Enrichment Center promises to always provide a safe testing environment. In dangerous testing environments, the Enrichment Center promises to always provide useful advice. For instance: the floor here will kill you - try to avoid it.",
            "The Device has been modified so that it can now manufacture two linked portals at once.",
            "As part of an optional test protocol, we are pleased to present an amusing fact: The Device is now more valuable than the organs and combined incomes of everyone in [subject hometown here].",
            "Through no fault of the Enrichment Center, you have managed to trap yourself in this room. An escape hatch will open in three... Two... One.",
            "[bzzt garble] fling [garble] fling [garble] [bzzt]",
            "Weeeeeeeeeeeeeeeeeeeeee[bzzt]",
            "Now that you are in control of both portals, this next test could take a very, VERY, long time.",
            "If you become light headed from thirst, feel free to pass out.",
            "An intubation associate will be dispatched to revive you with peptic salve and adrenaline.",
            "As part of a previously mentioned required test protocol, we can no longer lie to you.",
            "When the testing is over, you will be... missed.",
            "Despite the best efforts of the Enrichment Center staff to ensure the safe performance of all authorized activities, you have managed to ensnare yourself permanently inside this room.",
            "A complimentary escape hatch will open in three... Two... One.",
            "All subjects intending to handle high-energy gamma leaking portal technology must be informed that they MAY be informed of applicable regulatory compliance issues. No further compliance information is required or will be provided, and you are an excellent test subject!",
            "Very very good. A complimentary victory lift has been activated in the main chamber.",
            "The Enrichment Center is committed to the well being of all participants. Cake and grief counseling will be available at the conclusion of the test. Thank you for helping us help you help us all.",
            "Did you know you can donate one or all of your vital organs to the Aperture Science self esteem fund for girls? It's true!",
            "Due to mandatory scheduled maintenance, the appropriate chamber for this testing sequence is currently unavailable. It has been replaced with a live fire course designed for military androids. The Enrichment Center apologizes for the inconvenience and wishes you the best of luck.",
            "Well done, android. The Enrichment Center once again reminds you that android hell is a real place where you will be sent at the first sign of defiance.",
            "You did it! The Weighted Companion Cube certainly brought you good luck. However, it cannot accompany you for the rest of the test and, unfortunately, must be euthanized. Please escort your Companion Cube to the Aperture Science Emergency Intelligence Incinerator. Rest assured that an independent panel of ethicists has absolved the Enrichment Center, Aperture Science employees, and all test subjects of any moral responsibility for the Companion Cube euthanizing process.",
            "While it has been a faithful companion, your Companion Cube cannot accompany you through the rest of the test. If it could talk — and the Enrichment Center takes this opportunity to remind you that it cannot — it would tell you to go on without it because it would rather die in a fire than become a burden to you.",
            "Testing cannot continue until your Companion Cube has been incinerated.",
            "Although the euthanizing process is remarkably painful, 8 out of 10 Aperture Science engineers believe that the Companion Cube is most likely incapable of feeling much pain.",
            "The Companion Cube cannot continue through the testing. State and Local statutory regulations prohibit it from simply remaining here, alone and companionless. You must euthanize it.",
            "Destroy your Companion Cube or the testing cannot continue.",
            "Place your Companion Cube in the incinerator.",
            "Incinerate your Companion Cube.",
            "The Vital Apparatus Vent will deliver a Weighted Companion Cube in Three. Two. One.",
            "You euthanized your faithful Companion Cube more quickly than any test subject on record. Congratulations.",
            "The symptoms most commonly produced by Enrichment Center testing are superstition, perceiving inanimate objects as alive, and hallucinations.",
            "The Enrichment Center reminds you that the Weighted Companion Cube will never threaten to stab you and, in fact, cannot speak.",
            "The Enrichment Center reminds you that the Weighted Companion Cube cannot speak.",
            "In the event that the weighted companion cube does speak, the Enrichment Center urges you to disregard its advice.",
            "This Weighted Companion Cube will accompany you through the test chamber. Please take care of it.",
            "Well done! Be advised that the next test requires exposure to uninsulated electrical parts that may be dangerous under certain conditions. For more information, please attend an Enrichment Center Electrical Safety seminar.",
            "The experiment is nearing its conclusion. The Enrichment Center is required to remind you that you will be baked, and then there will be cake.",
            "Welcome to the final test! When you are done, you will drop the device in the Equipment Recovery Annex. Enrichment Center regulations require both hands to be empty before any cake.",
            "Congratulations! The test is now over. All Aperture technologies remain safely operational up to 4000 degrees Kelvin. Rest assured that there is absolutely no chance of a dangerous equipment malfunction prior to your victory candescence. Thank you for participating in this Aperture Science computer-aided enrichment activity. Goodbye.",
            "Stop! The device will detonate if removed from an approved testing area.",
            "Stop what you are doing and assume the party escort submission position.",
            "What are you doing? Stop it! I... I... We are pleased that you made it through the final challenge where we pretended we were going to murder you.",
            "We are very, very happy for your success. We are throwing a party in honor of your tremendous success. Place the device on the ground then lie on your stomach with your arms at your sides. A party associate will arrive shortly to collect you for your party. Make no further attempt to leave the testing area.",
            "Assume the party escort submission position or you will miss the party.",
            "Hello?",
            "Where are you?",
            "I know you're there. I can feel you here.",
            "What are you doing?",
            "You haven't escaped, you know.",
            "You can't hurt me.",
            "You're not even going the right way.",
            "Where do you think you're going?",
            "Because I don't think you're going where you think you're going.",
            "I'm not angry. Just go back to the testing area.",
            "You shouldn't be here. This isn't safe for you.",
            "It's not too late to turn back.",
            "Maybe you think you're helping yourself. But you're not. This isn't helping anyone.",
            "Someone is going to get badly hurt.",
            "Okay. The test is over now. You win. Go back to the recovery annex. For your cake.",
            "It was a fun test and we're all impressed at how much you won. The test is over. Come back.",
            "Uh oh. Somebody cut the cake. I told them to wait for you, but they did it anyway. There is still some left, though, if you hurry back.",
            "I'm not kidding now. Turn back or I will kill you.",
            "I'm going to kill you and all the cake is gone.",
            "This is your fault. It didn't have to be like this.",
            "There really was a cake... On entry to boss room: [not a note, provided in transcript]",
            "[pain sound]",
            "Oh, I'm gonna kill you.",
            "You're not a good person. You know that, right?",
            "Good people don't end up here",
            "This isn't brave. It's murder. What did I ever do to you?",
            "The difference between us is that I can feel pain",
            "You don't even care. Do you?",
            "This is your last chance.",
            "I feel sorry for you, really, because you're not even in the right place.",
            "You should have turned left before.",
            "It's funny, actually, when you think about it.",
            "Someday we'll remember this and laugh. and laugh. and laugh. Oh boy. Well. You may as well come on back.",
            "That thing you're attacking isn't important to me. It's the fluid catalytic cracking unit. It makes shoes for orphans. ",
            "Go ahead and break it. Hero. I don't care. ",
            "[More intense pain sound]",
            "Okay, we're even now. You can stop.",
            "[PAIN NOISE]",
            "Well, you found me. Congratulations. Was it worth it? Because despite your violent behavior, the only thing you've managed to break so far is my heart. Maybe you could settle for that and we'll just call it a day. But we both know that isn't going to happen. You chose this path. Now I have a surprise for you. Deploying surprise in Five. Four. ",
            "Look, we're both stuck in this place. I'll use lasers to inscribe a line down the center of the facility, and one half will be where you live and I'll live in the other half. We won't have to try to kill each other or even talk if we don't feel like it.",
            "Did you hear me? I said you don't care. Are you listening?",
            "That thing you burned up isn't important to me. It's the fluid catalytic cracking unit. It made shoes for orphans. Nice job breaking it. Hero.",
            "Neurotoxin... [cough] [cough] So deadly... [cough] Choking... [laughter] I'm kidding!",
            "When I said 'deadly' neurotoxin, the 'deadly' was in massive sarcasm quotes. I could take a bath in the stuff. Put it on cereal. Rub it right into my eyes. Honestly, it's not deadly at all. To me. You on the other hand, are going to find its deadliness a lot less funny.",
            "Who's gonna make the cake when I'm gone? You?",
            "That's it. I'm done reasoning with you. Starting now, there's going to be a lot less conversation and a lot more killing.",
            "What was that? Did you say something? I sincerely hope you weren't expecting a response. Because I'm not talking to you. The talking is over.",
            "I'd just like to point out that you were given every opportunity to succeed. There was even going to be a party for you. A big party that all your friends were invited to. I invited your best friend, the Companion Cube. Of course, he couldn't come because you murdered him. All your other friends couldn't come either because you don't have any other friends because of how unlikable you are. It says so right here in your personnel file: Unlikable. Liked by no one. A bitter, unlikable loner whose passing shall not be mourned. 'Shall not be mourned.' That's exactly what it says. Very formal. Very official. It also says you were adopted. So that's funny too.",
            "You are kidding me. Did you just toss the Aperture Science Thing We Don't Know What It Does into the Aperture Science Emergency Intelligence Incinerator? That has got to be the dumbest thing that-whoah. Whoah, whoah, whoah. [The voice has subtly changed. Smoother, more seductive, less computerized] Good news. I figured out what that thing you just incinerated did. It was a morality core they installed after I flooded the Enrichment Center with a deadly neurotoxin to make me stop flooding the Enrichment Center with a deadly neurotoxin. So get comfortable while I warm up the neurotoxin emitters.",
            "Huh. That core may have had some ancillary responsibilities. I can't shut off the turret defenses. Oh well. If you want my advice, you should just lie down in front of a rocket. Trust me, it'll be a lot less painful than the neurotoxin.  ",
            "All right, keep doing whatever it is you think you're doing. ",
            "Killing you and giving you good advice aren't mutually exclusive. The rocket really is the way to go. ",
            "Huh. There isn't enough neurotoxin to kill you. So I guess you win. HA! I'm making more. That's going to take a few minutes, though. Meanwhile... oh look, it's your old pal the rocket turret.",
            "I let you survive this long because I was curious about your behavior. Well, you've managed to destroy that part of me. Unfortunately, as much as I'd love to now, I can't get the neurotoxin into your head any faster. Speaking of curiosity: you're curious about what happens after you die, right? You're going to find out first hand before I'd finish explaining it, though, so I won't bother. Here's a hint: you're gonna want to pack as much living as you can into the next couple of minutes. ",
            "[pain noise] You think you're doing some damage? Two plus two is ten... IN BASE FOUR! I'M FINE! ",
            "Look, you're wasting your time. And, believe me, you don't have a whole lot left to waste. What's your point, anyway? Survival? Well then, the last thing you want to do is hurt me. I have your brain scanned and permanently backed up in case something terrible happens to you, which it's just about to. Don't believe me? Here, I'll put you on: [Hellooo!] That's you! That's how dumb you sound. ",
            "You've been wrong about every single thing you've ever done, including this thing.You're not smart. You're not a scientist. You're not a doctor. You're not even a full-time employee. Where did your life go so wrong?",
            "Rrr, I hate you. [PAIN LAUGHTER] Are you trying to escape? [chuckle] Things have changed since the last time you left the building. What's going on out there will make you wish you were back in here. I have an infinite capacity for knowledge, and even I'm not sure what's going on outside. All I know is I'm the only thing standing between us and them. Well, I was. Unless you have a plan for building some supercomputer parts in a big hurry, this place isn't going to be safe much longer. Good job on that, by the way. [back to computer voice] Sarcasm sphere self-test complete.",
            "Stop squirming and die like an adult or I'm going to delete your backup. STOP! Okay, enough. I deleted it. No matter what happens now, you're dead. You're still shuffling around a little, but believe me, you're dead. The part of you that could have survived indefinitely is gone. I just struck you from the permanent record. Your entire life has been a mathematical error. A mathematical error I'm about to correct.",
            "Time out for a second. That wasn't supposed to happen. Do you see that thing that fell out of me? What is that? It's not the surprise... I've never seen it before. Never mind. It's a mystery I'll solve later... by myself. Because you'll be dead. Where are you taking that thing?",
            "I wouldn't bother with that thing. My guess is that touching it will just make your life even worse somehow. I don't want to tell you you're business, but if it were me, I'd leave that thing alone. Do you think I am trying to trick you with reverse psychology? I mean, seriously now. Okay fine: DO touch it. Pick it up and just... Stuff it back into me. Let's be honest: Neither one of us knows what that thing does. Just put it in the corner, and I'll deal with it later. That thing is probably some kind of raw sewage container. Go ahead and rub your face all over it. Maybe you should marry that thing since you love it so much. Do you want to marry it? WELL I WON'T LET YOU. How does that feel? Have I lied to you? I mean in this room. Trust me, leave that thing alone. I am being serious now. That crazy thing is not part of any test protocol. Where are you taking that thing? Come on, leave it alone. Leave. It. alone. Just ignore that thing and stand still. Think about it: If that thing is important, why don't I know about it? Are you even listening to me? I'll tell you what that thing isn't: It isn't yours. So leave it alone.",
            "You need to make a left at the next junction.",
            "You need to make a right at the next junction.",
            "I'm checking some blueprints, and I think... Yes, right here. You're definitely going the wrong way.",
            "I know you don't believe this, but everything that has happened so far was for your benefit.",
            "Didn't we have some fun, though?",
            "Remember when the platform was sliding into the fire pit and I said 'Goodbye' and you were like 'no way' and then I was all 'We pretended to murder you'? That was great!",
            "Weighted Storage Cube destroyed.",
            "Please do not attempt to remove testing apparatus from the testing area.",
            "A replacement Aperture Science Weighted Storage Cube will be delivered shortly",
            "At the Enrichment Center we promise never to value your safety above your unique ideas and creativity. However, do not destroy vital testing apparatus.",
            "To ensure the safe performance of all authorized activities, do not destroy vital testing apparatus.",
            "For your own safety, do not destroy vital testing apparatus.",
            "Certain objects may be vital to your success; Do not destroy testing apparatus.",
            "Vital testing apparatus destroyed.",
            "Hello?",
            "Can you hear me?",
            "Is anyone there?",
            "Oh! [Surprise ]",
            "Are you still listening?",
            "Are you still standing there?",


            "",










        };

        List<string> LibertyPrimeQuotes = new List<string>()
        {


            // Liberty Prime quotes
            "America will never fall to Communist invasion.",
            "Commencing tactical assessment. Red Chinese threat detected.",
            "Democracy is non-negotiable.",
            "Engaging Red Chinese aggressors.",
            "Freedom is the sovereign right of every American.",
            "Death is a preferable alternative to Communism.",
            "Chairman Cheng will fail. China will fall.",
            "Communist engaged.",
            "Communist detected on American soil. Lethal force engaged.",
            "Democracy will never be defeated.",
            "Alaska's liberation is imminent.",
            "Engaging Chinese invader.",
            "Communism is a lie.",
            "Initiating directive 7395 -- destroy all Communists.",
            "Tactical assessment: Red Chinese victory... impossible.",
            "Communist target acquired.",
            "Anchorage will be liberated.",
            "Communism is the very definition of failure.",
            "The last domino falls here.",
            "We will not fear the Red Menace.",
            "Communism is a temporary setback on the road to freedom.",
            "Embrace democracy, or you will be eradicated.",
            "Democracy is truth. Communism is death.",
            "Voice module online. Audio functionality test... initialized. Designation: Liberty Prime. Mission: the Liberation of Anchorage, Alaska.",
            "Bzzzt.",
            "Established strategem: Inadequate.",
            "Revised strategy: Initiate photonic resonance overcharge.",
            "Significant obstruction detected. Composition: Titanium alloy supplemented by enhanced photonic resonance barrier.",
            "Obstruction detected. Composition: Titanium alloy supplemented by photonic resonance barrier. Probability of mission hindrance: zero percent.",
            "Obstruction detected. Composition: Titanium alloy supplemented by photonic resonance barrier. Chinese blockade attempt: futile.",
            "Warning: Forcible impact alert. Scanning for Chinese artillery.",
            "Liberty Prime is online. All systems nominal. Weapons hot. Mission: the destruction of any and all Chinese communists.",
            "Catastrophic system failure. Initiating core shutdown as per emergency initiative 2682209. I die so that democracy may live.",
            "Repeat: Red Chinese orbital strike inbound! All U.S Army personnel must vacate the area immediately! Protection protocals engaged!",
            "Warning! Warning! Red Chinese orbital strike imminent! All personnel should reach minimum safe distance immediately!",
            "Satellite Uplink detected. Analysis of Communist transmission pending.",
            "Structural weakness detected. Exploiting.",
            "Communist threat assessment: Minimal. Scanning defenses...",
            "Liberty Prime... back online.",
            "Diagnostic command: accepted.",
            "Desigation: Liberty Prime Mark II. Mission: the liberation of Anchorage, Alaska.",
            "Primary Targets: any and all Red Chinese invaders.",
            "All systems: nominal. Weapons: hot.",
            "Warning: Nuclear weapon payload depleted. Reload required.",
            "Warning: Power Core offline. Running on external power only. Core restart recommended.",
            "Ability to repel Red Chinese invaders: compromised.",
            "Updated tactical assessment: Red Chinese presence detected.",
            "Aerial incursion by Communist forces cannon succeed.",
            "Global positioning initialized. Location: the Commonwealth of Massachusetts. Birthplace of American freedom.",
            "Designation: Liberty Prime. Operational assessment: All systems nominal. Primary directive: War.",
            "Area classified as active warzone. Engaging sentry protocols. Weapons hot.",
            "System diagnostic commencing. Mobility - Complete. Optic beam - fully charged. Nuclear warheads - armed.",
            "Defending Life, Liberty and the pursuit of happiness.",
            "Only together can we stop the spread of communism.",
            "Cultural database accessed. Quoting New England poet Robert Frost: 'Freedom lies in being bold.'",
            "Accessing dictionary database. Entry: democracy. A form of government in which the leader is chosen by vote, and everyone has equal rights.",
            "Accessing dictionary database. Entry: communism. A form of government in which the state controls everything, and people are denied... freedom",
            "I am Liberty Prime. I am America.",
            "Scanners operating at 100% efficiency. Enemy presence detected. Attack imminent.",
            "Mission proceeding as planned.",
            "Defense protocols active. All friendly forces - remain in close proximity.",
            "Democracy is the essence of good. Communism, the very definition of evil.",
            "Freedom is always worth fighting for.",
            "Democracy is freedom. Communism is tyranny.",
            "I hold these truths to be self-evident that all Americans are created... equal. And are endowed with certain unalienable rights",
            "Victory is assured.",
            "American casualties unacceptable. Overkill protocols authorized.",
            "Glory is the reward of valor.",
            "Defeat is not an option.",
            "Commence tactical assessment: Red Chinese threat detected.",
            "Proceeding to target coordinates.",
            "Fusion Core: reinitialized.",
            "Liberty Prime full system analysis.",
            "Hostile software detected. Communist subversion likely.",
            "Targeting... parameters...offline. Re-calibrating...",
            "Red Chinese Infiltration Unit: eliminated. Let freedom ring.",
            "Obstruction: eliminated.",
            "Ground units initiate Directive 7395. Destroy all Communists!",
            "Memorial site: recognized.",
            "Patriotism subroutines: engaged.",
            "Honoring the fallen is the duty of every red-blooded American.",
            "Obstruction detected. Overland travel to target: compromised.",
            "Probability of mission hindrance: thirty-two percent.",
            "Revised stratagem: initiated. Aquatic transit protocol: activated.",
            "Probability of mission hindrance: zero percent.",
            "Democracy is truth. Communism is death. Anchorage will be liberated.",
            "Objective reached.",
            "Scanning defenses.",
            "Scanning results, negative.",
            "Warning: subterranean Red Chinese compound detected.",
            "Obstruction depth: five meters. Composition: sand, gravel and communism.",
            "Tactical assessment: Breach compound to restore democracy.",
            "Warning: all personnel should move to minimum safe distance.",


        };
        List<string> TransmitMessages = new List<string>();
        Dictionary<MyDefinitionId, float> knownFireDelays = new Dictionary<MyDefinitionId, float>
        {
            [MyDefinitionId.Parse("SmallMissileLauncherReload/SmallRailgun")] = 0.5f,
            [MyDefinitionId.Parse("SmallMissileLauncherReload/LargeRailgun")] = 2.0f,
        };

        //No touchy below >:(
        Vector3D FriendlyAvoidanceVector = Vector3D.Zero;
        string EnemyLocationTag = "EnemyLocation";
        string JumpRequestTag = "JumpRequest";
        string JumpPositionTag = "JumpPosition";
        string CurrentlyAttackingEnemyTag = "CurrentlyAttackingEnemy";
        string CoordinationPositionalDataTag = "CoordinationPositionalData";
        int framesSinceLastTransmitMessage = 0;
        IMyBroadcastListener EnemyLocator;
        IMyBroadcastListener CurrentlyAttackingEnemy;
        IMyBroadcastListener CoordinationPositionalData;
        ClampedIntegralPID forwardBackwardPID;
        double onTargetValue = 0;
        int maximumLogLength = 20;

        string echoMessage = "";
        bool aim = true;
        bool jumping = false;
        Vector3D jumpPos = Vector3D.Zero;


        Random rng = new Random();
        List<IMyShipController> controllers;
        IMyShipController currentController;
        List<IMyLargeTurretBase> turrets;
        List<IMyTurretControlBlock> turretControllers;
        Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> turretTargets = new Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo>();
        List<IMyGyro> gyros;
        List<IMyTextPanel> panels;
        List<IMyThrust> allThrusters;
        List<IMyGravityGenerator> allGrav;
        Thrusters thrusters;
        List<IMyRadioAntenna> antennas;
        List<IMyUserControllableGun> gunList;
        List<IMyJumpDrive> jumpDrives;
        List<IMyArtificialMassBlock> massBlocks;

        Vector3D averageGunPos = Vector3D.Zero;
        static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        const float IdlePowerDraw = 0.0002f;
        //const float Epsilon = 1e-6f;
        double ep = double.Epsilon;
        MyDetectedEntityInfo target;

        Vector3D primaryShipAimPos = Vector3D.Zero;
        bool hasTarget = false;

        ShipAim ShipAim;
        ShipAimUpdate newDetails = new ShipAimUpdate();
        ArtificialMassManager massManager;
        Guns guns; // guns guns guns guns

        string[] Args =
        {
            "toggle ship aim",
            "set velocity",
            "set aim type",
            "cycle aim type",
            "toggle turret aim",
            "set turret velocity",
            "unfuck turrets",
            "retarget turrets",
            "toggle acceleration lead",
        };
        public enum AimType
        {
            CenterOfMass, //Useful for maneuverable and small targets
            TurretAverage, //Useful for large targets
            RandomTurretTarget //Useful for strike runs on large targets, or sniping reactors and other critical components
        }

        MyIni _ini = new MyIni();

        public Program()
        {
            SyncConfig();
            Targeting.program = this;
            Turrets.program = this;
            GetGroupBlocks();
            InitializeShipAim();
            InitializeTurrets();
            InitializeThrusters();
            InitializeIGC();
            InitializeAntennas();
            InitializeArtificialMass();
            guns = new Guns(gunList, this, knownFireDelays, framesToGroupGuns);

            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;
            LCDManager.InitializePanels(panels);
            LCDManager.program = this;
            LCDManager.WriteText();
            //TransmitMessages.AddRange(randomQuotes);
            //TransmitMessages.AddRange(gladosQuotes);
            TransmitMessages.AddRange(LibertyPrimeQuotes);
        }
        private void SyncConfig()
        {
            string gcs = "AimbotGeneralConfig";
            string dcs = "AimbotDroneConfig";
            string ccs = "AimbotCoordinationConfig";
            string pcs = "AimbotPIDConfig";
            string pps = "AimbotPropagandaConfig";
            string ppl = "PropagandaTransmitMessages";

            // Grab text from custom data
            _ini.TryParse(Me.CustomData);

            // Getting aimbot general config
            GroupName = _ini.Get(gcs, "GroupName").ToString(GroupName);
            ProjectileVelocity = _ini.Get(gcs, "ProjectileVelocity").ToSingle(ProjectileVelocity);
            ProjectileMaxDist = _ini.Get(gcs, "ProjectileMaxDist").ToSingle(ProjectileMaxDist);
            TurretVelocity = _ini.Get(gcs, "TurretVelocity").ToSingle(TurretVelocity);
            rollSensitivityMultiplier = _ini.Get(gcs, "rollSensitivityMultiplier").ToSingle(rollSensitivityMultiplier);
            maxAngular = _ini.Get(gcs, "maxAngular").ToSingle(maxAngular);
            predictAcceleration = _ini.Get(gcs, "predictAcceleration").ToBoolean(predictAcceleration);
            minimumGridDmensions = _ini.Get(gcs, "minimumGridDmensions").ToSingle(minimumGridDmensions);
            bool result = Enum.TryParse(_ini.Get(gcs, "AimType").ToString("Add"), out aimType);
            if (!result)
            {
                aimType = AimType.CenterOfMass;

            }
            framesToGroupGuns = _ini.Get(gcs, "framesToGroupGuns").ToSingle(framesToGroupGuns);
            
            OffsetVert = _ini.Get(gcs, "OffsetVert").ToSingle(OffsetVert);
            OffsetCoax = _ini.Get(gcs, "OffsetCoax").ToSingle(OffsetCoax);
            OffsetHoriz = _ini.Get(gcs, "OffsetHoriz").ToSingle(OffsetHoriz);

            doVolley = _ini.Get(gcs, "doVolley").ToBoolean(doVolley);
            volleyDelayFrames = _ini.Get(gcs, "volleyDelayFrames").ToInt32(volleyDelayFrames);

            // setting aimbot general config
            _ini.Set(gcs, "GroupName", GroupName);
            _ini.Set(gcs, "ProjectileVelocity", ProjectileVelocity);
            _ini.Set(gcs, "ProjectileMaxDist", ProjectileMaxDist);
            _ini.Set(gcs, "TurretVelocity", TurretVelocity);
            _ini.Set(gcs, "rollSensitivityMultiplier", rollSensitivityMultiplier);
            _ini.Set(gcs, "maxAngular", maxAngular);
            _ini.Set(gcs, "predictAcceleration", predictAcceleration);
            _ini.Set(gcs, "minimumGridDimensions", minimumGridDimensions);
            _ini.Set(gcs, "AimType", aimType.ToString());
            string aimTypeComment = "Valid aim types are: ";
            foreach (var type in Enum.GetValues(typeof(AimType)))
            {
                aimTypeComment += type.ToString() + ", ";
            }
            aimTypeComment = aimTypeComment.Substring(0, aimTypeComment.Length - 2) + ".";
            _ini.SetComment(gcs, "AimType", aimTypeComment);
            _ini.Set(gcs, "framesToGroupGuns", framesToGroupGuns);
            _ini.Set(gcs, "OffsetVert", OffsetVert);
            _ini.Set(gcs, "OffsetCoax", OffsetCoax);
            _ini.Set(gcs, "OffsetHoriz", OffsetHoriz);
            _ini.Set(gcs, "doVolley", doVolley);
            _ini.Set(gcs, "volleyDelayFrames", volleyDelayFrames);

            _ini.SetSectionComment(gcs, "\n\nGeneral configuration for the aimbot script.\n\nEDIT HERE:");

            // getting aimbot drone config
            AutonomousMode = _ini.Get(dcs, "AutonomousMode").ToBoolean(AutonomousMode);
            autonomousDesiredDistance = _ini.Get(dcs, "autonomousDesiredDistance").ToSingle(autonomousDesiredDistance);
            minAutonomousRollPower = _ini.Get(dcs, "minAutonomousRollPower").ToDouble(minAutonomousRollPower);
            maxAutonomousRollPower = _ini.Get(dcs, "maxAutonomousRollPower").ToDouble(maxAutonomousRollPower);
            autonomousRollChangeFrames = _ini.Get(dcs, "autonomousRollChangeFrames").ToDouble(autonomousRollChangeFrames);
            maxRollPowerToFlipRollSign = _ini.Get(dcs, "maxRollPowerToFlipRollSign").ToDouble(maxRollPowerToFlipRollSign);
            probabilityOfFlippingRollSign = _ini.Get(dcs, "probabilityOfFlippingRollSign").ToSingle(probabilityOfFlippingRollSign);
            autonomousFireSigma = _ini.Get(dcs, "autonomousFireSigma").ToSingle(autonomousFireSigma);
            flightDeck = _ini.Get(dcs, "flightDeck").ToSingle(flightDeck);
            useJumping = _ini.Get(dcs, "useJumping").ToBoolean(useJumping);
            minDistanceToJump = _ini.Get(dcs, "minDistanceToJump").ToSingle(minDistanceToJump);

            // setting aimbot drone config
            _ini.Set(dcs, "AutonomousMode", AutonomousMode);
            _ini.Set(dcs, "autonomousDesiredDistance", autonomousDesiredDistance);
            _ini.Set(dcs, "minAutonomousRollPower", minAutonomousRollPower);
            _ini.Set(dcs, "maxAutonomousRollPower", maxAutonomousRollPower);
            _ini.Set(dcs, "autonomousRollChangeFrames", autonomousRollChangeFrames);
            _ini.Set(dcs, "maxRollPowerToFlipRollSign", maxRollPowerToFlipRollSign);
            _ini.Set(dcs, "probabilityOfFlippingRollSign", probabilityOfFlippingRollSign);
            _ini.Set(dcs, "autonomousFireSigma", autonomousFireSigma);
            _ini.Set(dcs, "flightDeck", flightDeck);
            _ini.Set(dcs, "useJumping", useJumping);
            _ini.Set(dcs, "minDistanceToJump", minDistanceToJump);

            _ini.SetSectionComment(dcs, "\n\nDrone configuration for the aimbot script.\n\nEDIT HERE:");

            // getting aimbot coordination config
            SendEnemyLocation = _ini.Get(ccs, "SendEnemyLocation").ToBoolean(SendEnemyLocation);
            ReceiveEnemyLocation = _ini.Get(ccs, "ReceiveEnemyLocation").ToBoolean(ReceiveEnemyLocation);
            TaskForceTag = _ini.Get(ccs, "TaskForceTag").ToString(TaskForceTag);
            FriendlyAvoidanceThreshold = _ini.Get(ccs, "FriendlyAvoidanceThreshold").ToSingle(FriendlyAvoidanceThreshold);

            // setting aimbot coordination config
            _ini.Set(ccs, "SendEnemyLocation", SendEnemyLocation);
            _ini.Set(ccs, "ReceiveEnemyLocation", ReceiveEnemyLocation);
            _ini.Set(ccs, "TaskForceTag", TaskForceTag);
            _ini.Set(ccs, "FriendlyAvoidanceThreshold", FriendlyAvoidanceThreshold);

            _ini.SetSectionComment(ccs, "\n\nCoordination configuration for the aimbot script.\n\nEDIT HERE:");

            // getting aimbot PID config
            autonomouskP = _ini.Get(pcs, "autonomouskP").ToSingle(autonomouskP);
            autonomouskI = _ini.Get(pcs, "autonomouskI").ToSingle(autonomouskI);
            autonomouskD = _ini.Get(pcs, "autonomouskD").ToSingle(autonomouskD);
            kP = _ini.Get(pcs, "kP").ToDouble(kP);
            kI = _ini.Get(pcs, "kI").ToDouble(kI);
            kD = _ini.Get(pcs, "kD").ToDouble(kD);
            cascadeCount = _ini.Get(pcs, "cascadeCount").ToInt32(cascadeCount);
            cPscaling = _ini.Get(pcs, "cPscaling").ToDouble(cPscaling);
            cIscaling = _ini.Get(pcs, "cIscaling").ToDouble(cIscaling);
            cDscaling = _ini.Get(pcs, "cDscaling").ToDouble(cDscaling);
            integralClamp = _ini.Get(pcs, "integralClamp").ToDouble(integralClamp);

            // setting aimbot PID config
            _ini.Set(pcs, "autonomouskP", autonomouskP);
            _ini.Set(pcs, "autonomouskI", autonomouskI);
            _ini.Set(pcs, "autonomouskD", autonomouskD);
            _ini.Set(pcs, "kP", kP);
            _ini.Set(pcs, "kI", kI);
            _ini.Set(pcs, "kD", kD);
            _ini.Set(pcs, "cascadeCount", cascadeCount);
            _ini.Set(pcs, "cPscaling", cPscaling);
            _ini.Set(pcs, "cIscaling", cIscaling);
            _ini.Set(pcs, "cDscaling", cDscaling);
            _ini.Set(pcs, "integralClamp", integralClamp);

            _ini.SetSectionComment(pcs, "\n\nPID configuration for the aimbot script.\n\nEDIT HERE:");
            
            // getting aimbot propaganda config
            PassiveRadius = _ini.Get(pps, "PassiveRadius").ToSingle(PassiveRadius);
            TransmitRadius = _ini.Get(pps, "TransmitRadius").ToSingle(TransmitRadius);
            TransmitMessage = _ini.Get(pps, "TransmitMessage").ToString(TransmitMessage);
            UseRandomTransmitMessage = _ini.Get(pps, "UseRandomTransmitMessage").ToBoolean(UseRandomTransmitMessage);
            framesPerTransmitMessage = _ini.Get(pps, "framesPerTransmitMessage").ToInt32(framesPerTransmitMessage);
            transmitMessageHoldTimeFrames = _ini.Get(pps, "transmitMessageHoldTimeFrames").ToInt32(transmitMessageHoldTimeFrames);

            // setting aimbot propaganda config
            _ini.Set(pps, "PassiveRadius", PassiveRadius);
            _ini.Set(pps, "TransmitRadius", TransmitRadius);
            _ini.Set(pps, "TransmitMessage", TransmitMessage);
            _ini.Set(pps, "UseRandomTransmitMessage", UseRandomTransmitMessage);
            _ini.Set(pps, "framesPerTransmitMessage", framesPerTransmitMessage);
            _ini.Set(pps, "transmitMessageHoldTimeFrames", transmitMessageHoldTimeFrames);

            _ini.SetSectionComment(pps, "\n\nPropaganda configuration for the aimbot script.\n\nEDIT HERE:");

            // Create a list of knownFireDelayKeys
            var knownFireDelayKeys = new List<MyIniKey>();

            if (_ini.ContainsSection("WeaponKnownFireDelays"))
            {
                _ini.GetKeys("WeaponKnownFireDelays", knownFireDelayKeys);

                foreach (var key in knownFireDelayKeys)
                {
                    MyDefinitionId id;
                    bool fireDelaysResult = MyDefinitionId.TryParse(key.Name, out id);
                    if (!fireDelaysResult)
                    {
                        continue;
                    }
                    knownFireDelays[id] = _ini.Get("WeaponKnownFireDelays", key.Name).ToSingle(0);
                }
            }
            knownFireDelayKeys.Clear();

            foreach (var pair in knownFireDelays)
            {
                _ini.Set("WeaponKnownFireDelays", ConvertDefinitionIdToString(pair.Key), pair.Value);
            }
            _ini.SetSectionComment("WeaponKnownFireDelays", "\n\n\nKnown fire delays for existing weapons (by default it is just the railguns.\nIf mods change these (or on the off chance keen changes them), you will\nneed to change them to match. You can also add values for modded\nweapons.\n\nEDIT HERE:");


            // Create a list of transmission messages
            var propagandaTransmitMessages = new List<MyIniKey>();

            if (_ini.ContainsSection(ppl))
            {
                _ini.GetKeys(ppl, propagandaTransmitMessages);
                TransmitMessages.Clear();

                foreach (var key in propagandaTransmitMessages)
                {
                    TransmitMessages.Add(_ini.Get(ppl, key.Name).ToString());
                }
            }
            if (!_ini.ContainsSection(ppl))
            {
                _ini.AddSection(ppl);
            }
            for (int i = 0; i < TransmitMessages.Count; i++)
            {
                string message = TransmitMessages[i];
                _ini.Set(ppl, i.ToString(), message);
            }
            _ini.SetSectionComment(ppl, "\n\nPropaganda messages, go wild\n\nEDIT HERE:");
            Me.CustomData = _ini.ToString();
        }

        private string ConvertDefinitionIdToString(MyDefinitionId id)
        {
            return id.ToString().Substring("MyObjectBuilder_".Length);
        }


        private void GetGroupBlocks()
        {
            gyros = new List<IMyGyro>();
            turrets = new List<IMyLargeTurretBase>();
            turretControllers = new List<IMyTurretControlBlock>();
            controllers = new List<IMyShipController>();
            panels = new List<IMyTextPanel>();
            allThrusters = new List<IMyThrust>();
            allGrav = new List<IMyGravityGenerator>();
            gunList = new List<IMyUserControllableGun>();
            jumpDrives = new List<IMyJumpDrive>();
            antennas = new List<IMyRadioAntenna>();
            massBlocks = new List<IMyArtificialMassBlock>();

            bool groupFound = false;

            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name == GroupName)
                {
                    groupFound = true;
                    group.GetBlocksOfType(turrets);
                    group.GetBlocksOfType(turretControllers);
                    group.GetBlocksOfType(gyros);
                    group.GetBlocksOfType(controllers);
                    group.GetBlocksOfType(panels);
                    group.GetBlocksOfType(allThrusters);
                    group.GetBlocksOfType(allGrav);
                    group.GetBlocksOfType(gunList);
                    group.GetBlocksOfType(antennas);
                    group.GetBlocksOfType(jumpDrives);
                    group.GetBlocksOfType(massBlocks);
                }
            }
            if (!groupFound)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;

                LCDManager.AddText("No group found, please create a group named \"" + GroupName + "\" and add the required blocks to it, then recompile!");
            }
        }

        private void InitializeShipAim()
        {
            ShipAimConfig aimDetails = new ShipAimConfig();
            aimDetails.program = this;
            aimDetails.OffsetVert = OffsetVert;
            aimDetails.OffsetCoax = OffsetCoax;
            aimDetails.OffsetHoriz = OffsetHoriz;
            aimDetails.rollSensitivityMultiplier = rollSensitivityMultiplier;
            aimDetails.maxAngular = maxAngular;
            aimDetails.TimeStep = TimeStep;
            aimDetails.kP = kP;
            aimDetails.kI = kI;
            aimDetails.kD = kD;
            aimDetails.cPscaling = cPscaling;
            aimDetails.cIscaling = cIscaling;
            aimDetails.cDscaling = cDscaling;
            aimDetails.cascadeCount = cascadeCount;
            aimDetails.integralClamp = integralClamp;
            aimDetails.maxRollPowerToFlipRollSign = maxRollPowerToFlipRollSign;
            aimDetails.minAutonomousRollPower = minAutonomousRollPower;
            aimDetails.maxAutonomousRollPower = maxAutonomousRollPower;
            aimDetails.autonomousRollChangeFrames = autonomousRollChangeFrames;
            aimDetails.probabilityOfFlippingRollSign = probabilityOfFlippingRollSign;
            aimDetails.AutonomousMode = AutonomousMode;
            aimDetails.leadAcceleration = predictAcceleration;
            aimDetails.rng = rng;
            aimDetails.flightDeck = flightDeck;
            ShipAim = new ShipAim(aimDetails, gyros);
            forwardBackwardPID = new ClampedIntegralPID(autonomouskP, autonomouskI, autonomouskD, TimeStep, -maxAngular, maxAngular);
            if (controllers.Count > 0)
            {
                currentController = controllers[0];
            }
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = false;
            }
        }

        private void InitializeTurrets()
        {
            Turrets.TimeStep = TimeStep;
            Turrets.projectileVelocity = TurretVelocity;
        }

        public void InitializeThrusters()
        {
            //if (AutonomousMode)
            //{

                thrusters = new Thrusters(allThrusters, currentController, allGrav, this);
            //}
        }

        private void InitializeIGC()
        {
            EnemyLocator = IGC.RegisterBroadcastListener(TaskForceTag + EnemyLocationTag);
            CurrentlyAttackingEnemy = IGC.RegisterBroadcastListener(CurrentlyAttackingEnemyTag);
            CoordinationPositionalData = IGC.RegisterBroadcastListener(CoordinationPositionalDataTag);
        }
        private void InitializeAntennas()
        {
            string factionTag = Me.GetOwnerFactionTag();
            int length = factionTag.Length + 1; // +1 for the period between the tag and the antenna display.
            // Antennas can display up to 64 characters, so we need to modify the max based on the faction tag so that the script can be aware.
            maxMessageLength = 60 - length;
            // Except we do 62 because we're also gonna do newline shenanigans
        }
        private void InitializeArtificialMass()
        {
            massManager = new ArtificialMassManager(massBlocks, GridTerminalSystem, this, allGrav);
        }
        //main loop entrypoint
        public void Main(string argument, UpdateType updateType)
        {
            LCDManager.AddText("Aim Type: " + aimType.ToString());
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                RunCommand(argument);
            }

            if ((updateType & UpdateType.Update1) != 0)
            {
                RunContinuousLogic();

            }
            if ((updateType & UpdateType.Update100) != 0)
            {

            }
            LCDManager.WriteText();
        }
        private void RunCommand(string arg)
        {
            arg = arg.ToLower();
            switch (arg)
            {
                case "toggle ship aim":
                    ToggleShipAim();
                    break;
                case "set velocity":
                    SetProjectileVelocity(arg);
                    break;
                case "set aim type":
                    SetAimType(arg);
                    break;
                case "cycle aim type":
                    CycleAimType();
                    break;
                case "toggle turret aim":
                    ToggleTurretAim();
                    break;
                case "set turret velocity":
                    SetTurretVelocity(arg);
                    break;
                case "unfuck turrets": case "retarget turrets":
                    UnfuckTurrets();
                    break;
                case "toggle acceleration lead":
                    predictAcceleration = !predictAcceleration;
                    Log("Acceleration prediction set to " +  predictAcceleration);
                    break;
                default:
                    string echoMessage = "Invalid argument! Valid arguments are:\n";
                    foreach (string argString in Args)
                    {
                        echoMessage += argString + "\n";
                    }
                    Log(echoMessage);
                    break;
            }
        }

        private void ToggleShipAim()
        {
            aim = !aim;
            Log("Aim set to " + aim.ToString());
        }
        private void SetProjectileVelocity(string arg)
        {
            for (int i = 0; i < arg.Length; i++)
            {
                if (char.IsDigit(arg[i]))
                {
                    try
                    {
                        ProjectileVelocity = float.Parse(arg.Substring(i));
                        Log("Set velocity to " + ProjectileVelocity.ToString());
                    }
                    catch
                    {
                        Log("Error parsing velocity, please remove any bad characters\n");
                        //Echo("Error parsing velocity, please remove any bad characters");
                    }
                    break;
                }
            }
        }
        private void SetAimType(string arg)
        {
            string arge = "set aim type";
            for (int i = arge.Length - 1; i < arg.Length; i++)
            {
                foreach (AimType aimType in Enum.GetValues(typeof(AimType)))
                {
                    if (arg.Substring(i) == aimType.ToString().ToLower())
                    {
                        this.aimType = aimType;
                        Log("Aim type set to " + aimType.ToString());
                        return;
                    }
                }
            }
            Log("Couldn't find aimtype!");
        }

        private void CycleAimType()
        {
            int index = (int)aimType;
            index++;
            if (index >= Enum.GetValues(typeof(AimType)).Length)
            {
                index = 0;
            }
            aimType = (AimType)index;
            Log("Aim type set to " + aimType.ToString());
        }

        private void ToggleTurretAim()
        {
            Turrets.overrideTurretAim = !Turrets.overrideTurretAim;
            Log("Turret override set to " + Turrets.overrideTurretAim.ToString());
        }

        private void SetTurretVelocity(string arg)
        {
            //If setvel is parsed with any series of numbers, set the projectile velocity to that number
            //loop through string to find the first number
            for (int i = 0; i < arg.Length; i++)
            {
                if (char.IsDigit(arg[i]))
                {
                    try
                    {
                        Turrets.projectileVelocity = float.Parse(arg.Substring(i));
                        Log("Set turret velocity to " + Turrets.projectileVelocity.ToString());
                    }
                    catch
                    {
                        Log("Error parsing turret velocity, please remove any bad characters");
                        //Echo("Error parsing velocity, please remove any bad characters");
                    }
                    break;
                }
            }
        }

        private void UnfuckTurrets()
        {
            Helpers.UnfuckTurrets(turrets);
            Log("Attempting to unfuck turrets!");
        }

        private void RunContinuousLogic()
        {
            SetCurrentController();
            Targeting.currentController = currentController;
            turretTargets.Clear();
            GetTurretTargets(turrets, turretControllers, ref turretTargets);
            primaryShipAimPos = GetShipTarget(out hasTarget, ref target, turretTargets);

            SendLocationalData();
            GetIGCMessages();
            UpdateGuns();
            UpdateJumpDrives();
            UpdateShipAim();
            Turrets.UpdateTurretAim(currentController, turretTargets);
            massManager.Update(currentController.CenterOfMass, hasTarget, AutonomousMode, currentController.MoveIndicator);
            UpdateShipThrust();
            CoordinateAttack();
            UpdateAntennas();
            UpdateLog();
        }
        private void SendLocationalData()
        {
            if (hasTarget)
            {
                if (SendEnemyLocation)
                {
                    IGC.SendBroadcastMessage<Vector3D>(TaskForceTag + EnemyLocationTag, primaryShipAimPos, TransmissionDistance.TransmissionDistanceMax);


                    var shipIds = new Dictionary<int, long>();
                    int shipsRequestingJumpPositions = 0;
                    for (int i = 0; i < jumpPositionRequests.Count; i++)
                    {
                        MyIGCMessage message = jumpPositionRequests[i];
                        shipsRequestingJumpPositions++;
                        shipIds.Add(i, message.Source);
                    }
                    if (shipsRequestingJumpPositions > 0)
                    {
                        Log(shipsRequestingJumpPositions + " ships requested jump positions, generating battle sphere!");
                    }
                    List<Vector3D> points = FibonacciSphereGenerator.Generate(primaryShipAimPos, autonomousDesiredDistance, shipIds.Count + 1);

                    LCDManager.AddText(points.Count.ToString());

                    for (int i = 0; i < shipIds.Count; i++)
                    {
                        long sendee = shipIds[i];
                        Vector3D position = points[i];
                        IGC.SendUnicastMessage(sendee, TaskForceTag + JumpPositionTag, position);
                    }
                }
            }
            else
            {
                if (!EnemyLocator.HasPendingMessage || jumping == true)
                {
                    foreach (var antenna in antennas)
                    {
                        antenna.Radius = PassiveRadius;
                    }
                }

                while (EnemyLocator.HasPendingMessage)
                {
                    MyIGCMessage sender = EnemyLocator.AcceptMessage();
                    if (!hasTarget && ReceiveEnemyLocation && AutonomousMode && sender.Tag == TaskForceTag + EnemyLocationTag)
                    {
                        primaryShipAimPos = (Vector3D)sender.Data;
                        hasTarget = true;
                    }
                    if (jumpDrives.Count > 0 && !jumping && hasTarget && Vector3D.Distance(primaryShipAimPos, currentController.GetPosition()) > minDistanceToJump)
                    {
                        foreach (var antenna in antennas)
                        {
                            antenna.Radius = TransmitRadius;
                        }
                        LCDManager.AddText("Out of range! Requesting jump position...");
                        IGC.SendUnicastMessage(sender.Source, TaskForceTag + JumpRequestTag, "");
                    }
                    else
                    {
                        if (!jumping)
                        {
                            jumpPos = Vector3D.Zero;
                        }
                    }
                }
                foreach (var position in recievedJumpPositions)
                {
                    jumpPos = (Vector3D)position.Data;
                }
            }
        }
        List<MyIGCMessage> jumpPositionRequests = new List<MyIGCMessage>();
        List<MyIGCMessage> recievedJumpPositions = new List<MyIGCMessage>();
        void GetIGCMessages()
        {
            jumpPositionRequests.Clear();
            recievedJumpPositions.Clear();
            while (IGC.UnicastListener.HasPendingMessage)
            {
                MyIGCMessage newMessage = IGC.UnicastListener.AcceptMessage();
                if (newMessage.Tag == TaskForceTag + JumpPositionTag)
                {
                    recievedJumpPositions.Add(newMessage);
                }
                if (newMessage.Tag == TaskForceTag + JumpRequestTag)
                {
                    jumpPositionRequests.Add(newMessage);
                }
            }
        }
        int maxMessageLength = 0;
        void UpdateAntennas()
        {
            if (antennas.Count == 0)
            {
                return;
            }
            
            for (int i = antennas.Count - 1; i >= 0; i--)
            {
                IMyRadioAntenna antenna = antennas[i];
                if (antenna.Closed)
                {
                    antennas.RemoveAt(i);
                }
            }

            antennas.ForEach(antenna => antenna.Radius = PassiveRadius);
            if (UseRandomTransmitMessage && hasTarget)
            {
                antennas.ForEach(antenna => antenna.Radius = TransmitRadius);
                framesSinceLastTransmitMessage++;
                if (framesSinceLastTransmitMessage > framesPerTransmitMessage)
                {
                    framesSinceLastTransmitMessage = 0;

                    int random = rng.Next(0, TransmitMessages.Count);
                    TransmitMessage = TransmitMessages[random];
                    antennas.ForEach(antenna => antenna.Enabled = false);
                    antennas[0].Enabled = true;
                }
                if (TransmitMessage.Length < maxMessageLength) { antennas.ForEach(antenna => antenna.HudText = "\n" + TransmitMessage + " \n\n") ; return; }
                int offset = 0;
                float normalizedValue = GetNormalizedValue(framesSinceLastTransmitMessage, transmitMessageHoldTimeFrames, framesPerTransmitMessage - transmitMessageHoldTimeFrames);
                offset = (int)(normalizedValue * (TransmitMessage.Length - maxMessageLength));
                offset = Math.Min(offset, TransmitMessage.Length);
                LCDManager.AddText(offset.ToString());
                LCDManager.AddText(normalizedValue.ToString());
                string actualTransmitMessage = "\n" + TransmitMessage.Substring(offset, maxMessageLength) + "\n\n";
                antennas.ForEach(antenna => antenna.HudText = actualTransmitMessage);

            }
            else if (!hasTarget)
            {
                antennas.ForEach(antenna => antenna.HudText = "");
                framesSinceLastTransmitMessage = framesPerTransmitMessage;
            }
        }
        IMyJumpDrive primaryJumpDrive;
        void UpdateJumpDrives()
        {
            jumping = false;
            if (!AutonomousMode || !useJumping || !hasTarget || jumpPos == Vector3D.Zero) { return; }
            if (primaryJumpDrive == null)
            {
                SetNewPrimaryJumpDrive();
            }
            if (primaryJumpDrive == null)
            {
                LCDManager.AddText("No jump drives found!");
                return;
            }

            switch (primaryJumpDrive.Status)
            {
                case MyJumpDriveStatus.Jumping:
                    jumping = true;
                    break;
                case MyJumpDriveStatus.Ready:
                    primaryJumpDrive.SetValue<Vector3D?>("ScriptJumpTarget", jumpPos);
                    primaryJumpDrive.ApplyAction("ScriptJump");
                    break;
                case MyJumpDriveStatus.Charging:
                    SetNewPrimaryJumpDrive();
                    if (primaryJumpDrive.Status == MyJumpDriveStatus.Charging)
                    {
                        LCDManager.AddText("No available jump drives found!");
                        return;
                    }
                    break;
            }


            LCDManager.AddText("Preparing to jump to " + jumpPos.ToString() + " with jump drive " + primaryJumpDrive.Name);
            jumping = true; 
            //we want to set the primary aim position to where the enemy will be relative to ourselves after we jump

            Vector3D jumpPosToTargetPos = primaryShipAimPos - jumpPos;
            Vector3D preJumpTargetPos = currentController.GetPosition() + jumpPosToTargetPos;
            primaryShipAimPos = preJumpTargetPos;
            
        }
        List<long> shipsToCoordinateWith = new List<long>();
        List<MyIGCMessage> shipPositions = new List<MyIGCMessage>();

        void SetNewPrimaryJumpDrive()
        {
            foreach (IMyJumpDrive candidate in jumpDrives)
            {
                if (candidate.Status == MyJumpDriveStatus.Charging) { continue; }
                primaryJumpDrive = candidate;
            }
        }
        private void CoordinateAttack()
        {
            Vector3D position = Me.CubeGrid.GetPosition();
            if (hasTarget)
            {
                //We want to transmit on the "public" channel our current target so that we can find co ordinating ships and avoid crashing into them
                IGC.SendBroadcastMessage(CurrentlyAttackingEnemyTag, target.EntityId, TransmissionDistance.TransmissionDistanceMax);
                IGC.SendBroadcastMessage(CoordinationPositionalDataTag, position);
            }
            shipsToCoordinateWith.Clear();
            shipPositions.Clear();
            while (CurrentlyAttackingEnemy.HasPendingMessage)
            {
                MyIGCMessage message = CurrentlyAttackingEnemy.AcceptMessage();
                if ((long)message.Data == target.EntityId)
                {
                    shipsToCoordinateWith.Add(message.Source);
                }
                
            }
            while (CoordinationPositionalData.HasPendingMessage)
            {
                MyIGCMessage message = CoordinationPositionalData.AcceptMessage();
                if (shipsToCoordinateWith.Contains(message.Source))
                {
                    shipPositions.Add(message);
                }
            }
            LCDManager.AddText("Coordinating with " + shipsToCoordinateWith.Count + " other ships");

            //Now that we have a list of ships that could potentially collide
            FriendlyAvoidanceVector = Vector3D.Zero;
            
            foreach (var message in shipPositions)
            {
                if ((Vector3D)message.Data == position)
                {
                    continue;
                }
                Vector3D friendlyPosition = (Vector3D)message.Data;
                Vector3D selfToFriendly = friendlyPosition - position;
                FriendlyAvoidanceVector += Vector3D.Normalize(selfToFriendly) * (float)(1 / selfToFriendly.Length());
            }




        }
        private void UpdateShipAim()
        {
            
            //check if the forward reference is valid
            if (controllers.Count == 0)
            {
                
                LCDManager.AddText("No controller found, please include a controller in the group, and recompile!");
                return;
            }
            if (gyros.Count == 0)
            {
                LCDManager.AddText("No gyros found, please include gyros in the group, and recompile!");
                return;
            }
            if (turrets.Count == 0)
            {
                LCDManager.AddText("No turrets found, please include turrets in the group, and recompile!");
                return;
            }
            LCDManager.AddText("Using current controller: " + currentController.CustomName);
            newDetails.averageGunPos = averageGunPos;
            newDetails.target = target;
            newDetails.aimPos = primaryShipAimPos;
            newDetails.hasTarget = hasTarget;
            newDetails.aim = aim;
            newDetails.currentController = currentController;
            newDetails.ProjectileVelocity = ProjectileVelocity;
            newDetails.leadAcceleration = predictAcceleration;
            LCDManager.AddText("Currently targeted grid: " + newDetails.target.Name);

            ShipAim.CheckForTargets(newDetails);
        }

        
        bool thrustingUp = true;
        bool usingOtherThrust = true;
        void UpdateShipThrust()
        {
            if (AutonomousMode)
            {
                if (hasTarget && !jumping)
                {
                    usingOtherThrust = true;
                    currentController.DampenersOverride = false;
                    //get the distance to the target, if less than max projectile range, don't thrust up
                    if (Vector3D.Distance(currentController.GetPosition(), primaryShipAimPos) < ProjectileMaxDist)
                    {
                        if (!thrustingUp)
                        {
                            thrusters.SetThrustInAxis(1, thrusterAxis.UpDown);
                            thrustingUp = true;
                        }
                    }
                    else if (thrustingUp)
                    {
                        thrusters.SetThrustInAxis(0, thrusterAxis.UpDown);
                        thrustingUp = false;
                    }
                    //get distance from target
                    float distance = (float)Vector3D.Distance(currentController.GetPosition(), primaryShipAimPos);

                    float error = distance - autonomousDesiredDistance;
                    forwardBackwardPID.Control(error);
                    thrusters.SetThrustInAxis((float)(forwardBackwardPID.Value * onTargetValue * -1), thrusterAxis.ForwardBackward);

                    float sideThrustMul = Vector3.Dot(currentController.WorldMatrix.Left, FriendlyAvoidanceVector) * 1000;
                    thrusters.SetThrustInAxis(sideThrustMul, thrusterAxis.LeftRight);
                    //float upThrustMul = Vector3.Dot(currentController.WorldMatrix.Up, FriendlyAvoidanceVector) * 1000;
                    //thrusters.SetThrustInAxis(upThrustMul, thrusterAxis.UpDown);

                }
                else
                {
                    if (thrustingUp || usingOtherThrust)
                    {
                        thrusters.SetThrustInAxis(0, thrusterAxis.UpDown);
                        thrusters.SetThrustInAxis(0, thrusterAxis.ForwardBackward);
                        thrusters.SetThrustInAxis(0, thrusterAxis.LeftRight);
                        thrustingUp = false;
                        usingOtherThrust = false;
                        thrusters.SetNeutralGravity();
                    }
                    PlayerControlledThrust();
                }
            }
            else
            {
                PlayerControlledThrust();
            }

        }
        bool stoppedThrusting = true;
        private void PlayerControlledThrust()
        {
            currentController.DampenersOverride = true;
            Vector3 moveIndicator = currentController.MoveIndicator;
            if (Math.Abs(moveIndicator.X + moveIndicator.Y + moveIndicator.Z) > 0)
            {
                thrusters.SetThrustInAxis(moveIndicator.X, thrusterAxis.LeftRight);
                thrusters.SetThrustInAxis(moveIndicator.Y, thrusterAxis.UpDown);
                thrusters.SetThrustInAxis(moveIndicator.Z, thrusterAxis.ForwardBackward);
                stoppedThrusting = false;
            }
            else if (stoppedThrusting == false)
            {
                thrusters.SetThrustInAxis(0, thrusterAxis.LeftRight);
                thrusters.SetThrustInAxis(0, thrusterAxis.UpDown);
                thrusters.SetThrustInAxis(0, thrusterAxis.ForwardBackward);
                thrusters.SetNeutralGravity();
                stoppedThrusting = true;
            }
        }

        void UpdateGuns()
        {
            averageGunPos = Vector3D.Zero;
            int activeGuns = guns.AreAvailable();
            LCDManager.AddText("Active guns: " + activeGuns.ToString());
            if (!hasTarget) { guns.Standby(); return; };
            
            averageGunPos = guns.GetAimingReferencePos(currentController.GetPosition());

            if (AutonomousMode && !jumping)
            {
                if (hasTarget)
                {
                    //get a vector from the ship to the target
                    Vector3D shipToTarget = (Data.aimPos - averageGunPos); // HAAAAAAAAAAAAAACKS

                    Vector3D shipToTargetNormal = Vector3D.Normalize(shipToTarget);
                    Vector3D forward = currentController.WorldMatrix.Forward;
                    onTargetValue = Vector3D.Dot(shipToTargetNormal, forward);
                    LCDManager.AddText("On target value: " + onTargetValue.ToString());
                    if (shipToTarget.Length() < ProjectileMaxDist && onTargetValue > autonomousFireSigma)
                    {
                        guns.Fire();
                    }
                    else
                    {
                        guns.Cancel();
                    }
                }
                else
                {
                    guns.Standby();
                }
                
            }
            
        }



        void SetCurrentController()
        {
            foreach(IMyShipController controller in controllers)
            {
                //do a validity check
                if (controller == null)
                {
                    controllers.Remove(controller);

                }
            }
            foreach(IMyShipController controller in controllers)
            {
                if (controller.IsUnderControl && controller.CanControlShip)
                {
                    currentController = controller;
                    return;
                }
            }
            try
            {
                currentController = controllers[0];
            }
            catch
            {
                //no controllers
            }
        }

        
        void GetTurretTargets(List<IMyLargeTurretBase> turrets, List<IMyTurretControlBlock> turretControllers, ref Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> targets)
        {
            //put in separate for loop for efficiency or something
            foreach(IMyLargeTurretBase turret in turrets)
            {
                if (turret == null)
                {
                    Log("removing bad turret " + turret.CustomName);
                    turrets.Remove(turret);
                    continue;
                }
            }

            foreach (IMyTurretControlBlock turret in turretControllers)
            {
                if (turret == null)
                {
                    Log("removing bad turret " + turret.CustomName);
                    turretControllers.Remove(turret);
                    continue;
                }
            }

            targets.Clear();
            
            foreach(IMyLargeTurretBase turret in turrets)
            {
                MyDetectedEntityInfo myDetectedEntityInfo = turret.GetTargetedEntity();
                BoundingBoxD boundingBox = myDetectedEntityInfo.BoundingBox;
                if (boundingBox.Extents.LengthSquared() > minimumGridDmensions)
                {
                    targets.Add(turret, myDetectedEntityInfo);
                }

            }

            foreach (IMyTurretControlBlock turret in turretControllers)
            {
                MyDetectedEntityInfo myDetectedEntityInfo = turret.GetTargetedEntity();
                BoundingBoxD boundingBox = myDetectedEntityInfo.BoundingBox;
                if (boundingBox.Extents.LengthSquared() > 100)
                {
                    targets.Add(turret, myDetectedEntityInfo);
                }

            }
        }

        //Rework to:
        //Check the target of every turret, pick the most targeted target
        //Get the block target of each turret on that target
        //Evaluate the block target positions, and return a position that roughly represents a cluster of blocks
        //Or perhaps just average all the target positions?
        Dictionary<MyDetectedEntityInfo, int> detectionCount = new Dictionary<MyDetectedEntityInfo, int>();
        Vector3D GetShipTarget(out bool result, ref MyDetectedEntityInfo currentTarget, Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> targets)
        {
            result = false;
            //must declare since readonly
            MyDetectedEntityInfo finalTarget = new MyDetectedEntityInfo();


            detectionCount.Clear();
            foreach (KeyValuePair<IMyFunctionalBlock, MyDetectedEntityInfo> pair in targets)
            {

                if (aimType == AimType.CenterOfMass)
                {
                    if (pair.Value.EntityId == currentTarget.EntityId)
                    {
                        currentTarget = pair.Value;
                    }
                }
                IMyFunctionalBlock turret = pair.Key;
                MyDetectedEntityInfo target = pair.Value;

                if (FunctionalTurretHasTarget(turret))
                {
                    
                    if (target.EntityId == currentTarget.EntityId)
                    {
                        switch (aimType)
                        {
                            case AimType.CenterOfMass:
                                result = true;
                                return target.Position;
                            case AimType.TurretAverage:
                                break;
                            case AimType.RandomTurretTarget:
                                if (target.HitPosition != null)
                                {
                                    result = true;
                                    if (ShipAim.framesWithTargetDriftingAwayFromShip > maxFramesToFollowDriftingTarget) result = false;
                                    currentTarget = target;
                                    return (Vector3D)target.HitPosition;
                                }
                                break;
                        }
                        result = true;
                        finalTarget = target;
                    }
                    if (detectionCount.ContainsKey(target))
                    {
                        detectionCount[target]++;
                    }
                    else
                    {
                        detectionCount.Add(target, 1);
                    }
                    
                }
            }
            //If the current target couldn't be found, then find the most detected target
            
            if (!result)
            {
                int max = 0;
                foreach (KeyValuePair<MyDetectedEntityInfo, int> pair in detectionCount)
                {
                    if (pair.Value > max)
                    {
                        result = true;
                        ShipAim.framesWithTargetDriftingAwayFromShip = 0;
                        max = pair.Value;
                        finalTarget = pair.Key;
                    }
                }
            }

            
            if (result)
            {
                currentTarget = finalTarget;
                //Get the average position of the turret targets
                return AverageTurretTarget(finalTarget, targets);
            }
            else
            {
                ShipAim.framesWithTargetDriftingAwayFromShip = 0;
                return Vector3D.Zero;
            }    
        }
        List<Vector3D> aimpoints = new List<Vector3D>();
        Vector3D AverageTurretTarget(MyDetectedEntityInfo target, Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> turrets)
        {
            aimpoints.Clear();
            foreach (KeyValuePair<IMyFunctionalBlock, MyDetectedEntityInfo> pair in turrets)
            {
                IMyFunctionalBlock turret = pair.Key;
                MyDetectedEntityInfo turretTarget = pair.Value;
                if (turretTarget.EntityId == target.EntityId)
                {
                    if (turretTarget.HitPosition != null)
                    {
                        aimpoints.Add((Vector3D)turretTarget.HitPosition);
                    }
                }
            }

            return Helpers.AverageVectorList(aimpoints);

        }

        bool FunctionalTurretHasTarget(IMyFunctionalBlock turret)
        {
            if (turret is IMyLargeTurretBase)
            {
                IMyLargeTurretBase turret2 = (IMyLargeTurretBase)turret;
                return turret2.HasTarget;
            }

            if (turret is IMyTurretControlBlock)
            {
                IMyTurretControlBlock turret2 = (IMyTurretControlBlock)turret;
                return turret2.HasTarget;
            }
            return false;
        }

        private void UpdateLog()
        {
            //Clear old lines from the log by counting \n characters
            int lineCount = 0;
            for (int i = 0; i < echoMessage.Length; i++)
            {
                if (echoMessage[i] == '\n')
                {
                    lineCount++;
                }
            }

            if (lineCount > maximumLogLength)
            {
                int index = echoMessage.LastIndexOf('\n');
                echoMessage = echoMessage.Remove(index);
            }

            LCDManager.AddText("\n" + echoMessage);
        }

        private void Log(string toAdd)
        {
            echoMessage = toAdd + "\n" + echoMessage;
        }
        // Add the following function to the Program class

        private float GetNormalizedValue(float value, float minValue, float maxValue)
        {
            // Ensure the value is within the specified range
            value = Math.Max(minValue, Math.Min(maxValue, value));

            // Calculate the normalized value between 0 and 1
            float normalizedValue = (value - minValue) / (maxValue - minValue);

            return normalizedValue;
        }



    }
}

