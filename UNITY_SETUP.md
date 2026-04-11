# Unity Setup Instructions

## 1. Create the project
- Open Unity Hub.
- Click **New project**.
- Choose the **3D Core** template.
- Create the project.

## 2. Add the script
- In your project, create the folder `Assets/Scripts` if it does not exist.
- Add `BasicFirstPersonController.cs` to `Assets/Scripts`.
- Let Unity compile.

## 3. Build the player object
- Create a **Capsule** in the scene and rename it to `Player`.
- Reset its transform to `(0, 1, 0)`.
- Add a **Character Controller** component to `Player`.
- Add the `BasicFirstPersonController` component to `Player`.

## 4. Create the camera rig
- Create a **Camera** as a child of `Player`.
- Rename it to `PlayerCamera`.
- Set its local position to `(0, 0.8, 0)`.
- Drag `PlayerCamera` into the script's `Camera Holder` field.

## 5. Tune movement
- Suggested starting values:
  - `Walk Speed`: `5`
  - `Sprint Speed`: `8`
  - `Acceleration`: `12`
  - `Gravity`: `-20`
  - `Jump Height`: `1.2`
  - `Mouse Sensitivity`: `2.2`
  - `Camera Smooth Time`: `0.05`

## 6. Play controls
- `W`, `A`, `S`, `D`: move
- Mouse: look around
- `Left Shift`: sprint
- `Space`: jump

## 7. Notes
- If the camera does not rotate, make sure `Camera Holder` is assigned.
- If movement feels too floaty, increase `Acceleration`.
- If mouse look feels too fast or too slow, adjust `Mouse Sensitivity`.

## 8. Add a monster
- Add `BasicMonsterController.cs` to `Assets/Scripts`.
- Create a **Capsule** or any 3D model in the scene and rename it to `Monster`.
- Add a **Character Controller** component to `Monster`.
- Add the `BasicMonsterController` component to `Monster`.
- Set the player's tag to **Player** so the monster can find it automatically.
- Place the monster a short distance away from the player.

## 9. Monster behavior
- `Move Speed` should be higher than the player's sprint speed if you want constant pressure. A good starting value is `9`.
- `Detection Radius` controls how far away the monster notices the player.
- `Stopping Distance` controls how close the monster gets before it stops moving.
- When the player enters the detection radius, the monster turns toward the player and chases.

## 10. Add puzzle interaction
- Add `PlayerInteractor.cs`, `SimplePuzzleInteractable.cs`, and `IInteractable.cs` to `Assets/Scripts`.
- Select the `Player` object and add the `PlayerInteractor` component.
- Assign `PlayerCamera` to the script's `Interaction Origin` field.

## 11. Create a puzzle object
- Create a 3D object such as a **Cube** and rename it to `PuzzleObject`.
- Add a collider if it does not already have one.
- Add the `SimplePuzzleInteractable` component.
- Set `Success Chance` to a value like `0.5` for a 50% success rate.

## 12. Failure signal event
- In the `SimplePuzzleInteractable` component, use the `On Failure` UnityEvent to send a signal when the random check fails.
- You can connect this event to any public method on another object, such as alerting the monster, playing a sound, or turning on a light.
- Press `E` while looking at the puzzle object and within interaction range to trigger the puzzle.

## 13. Add the game manager
- Add `GameManager.cs` to `Assets/Scripts`.
- Create an empty object in the scene named `GameManager`.
- Add the `GameManager` component to it.
- Set `Starting Phase` to `Playing` if gameplay should begin immediately.
- Set `Required Puzzle Count` to the number of puzzles the player must solve to win.

## 14. Win conditions
- Each `SimplePuzzleInteractable` can count toward the win condition through `Count Toward Win Condition`.
- When enough puzzles are solved, the `GameManager` changes the phase to `Won`.
- Use the `On Game Won` event to trigger a UI message, open a door, load another scene, or end the level.
- Use the `On Game Lost` event for fail states such as the monster catching the player.
