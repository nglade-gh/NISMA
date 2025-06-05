# Gerneral description
The model can be completely empty, or contain **grains**, **behaviours** and / or **fields**.  
Afterwards we have the simulation, containing **name**, **description** and optionally **agents**.
> Example of empty file
>> {"model":{},"simulation":{"name":"","description":""}}

# Model
## Grains
Each grain necessarily contains an **id** >0 (except the one with id 0 but it can be added manually without causing problems), followed by a **name** (string) and a color from a predetermined list. 
### optional settings 
The **icon** can be changed from the default square, in which case the setting is explicitly specified.\
If the agent is invisible, the boolean **invisble** appears and is set to true, and if the **size** is different from "1" it becomes explicit and has to be set between 0 and 5.\
A **description** (with no character limit) can be added. It is followed by **halfLife** >0 and **movementProbability** whose value is between 1 and 0.\
Finally, we have the list **allowedDirection** written explicitly but only if they're different from the default(left, up, right, down).\
More optional settings are unlocked after the creation of a field: **fieldProductions**, **fieldInfluences** and **fieldPermeable**. Each is a list of pairs of numbers, the first is an integer >0 which correlates to the field in question, the second a real number between 0 and 1 with up to eight digits après la virgule, representating the percentage of each value? idk
## Behaviours
Each behaviour has at least a **name** (string), followed by **probability** (between 0 and 1) if it's different from 1, and optionally **agePredicate** (>0) which is a two item list containing **op** associated to an operator, and **constant** followed by a positive integer . 
Then we have **mainReactiveId**, **mainProductId** and **sourceReactive** (all ints that are >0)\
> Example of behaviour if we have no agents initiated
>>"behaviours":[{"name":"Behaviour","sourceReactive":0}].  
>
>Raises an error message" Behaviour must have a main reactive "  

Then we have the **reaction**, which is a list containing **reactiveId** and **productId**, each followed by an int, along with a list of **allowedDirection** if it's different from the default.
Upon the creation of at least one field, optional settings can be added.  
First the **fieldPredicates** which comes right after the **name**, TO BE FINISHED WHEN THE WRATH OF GOD IS OVER\
Second and third options added are the **field prductions** and **fieldInfluences** which are a list each where each element is the field id associated to a value between 0 and 1.

## Fields
Fields themselves are optional, available when expert mode is toggled on. Similar to grains, the first id: 0 doesn't necessarily appear but can be added manually.
Each field has an **id**, a **name** and a **color**.
Optionally, they can also have a list of **allowedDirection**, a **speed** (between 0 and 1) and a **halfLife** (int >0).

# Simulation
The simulation must contain a **name** and a **description**, even if they're empty.\
If any grains are placed then it can also contain **agents** which a list of numbers > -1, of size = to grid size where each number corresponding to the **id** of the agent occupying each case, with -1 being an empty case.

