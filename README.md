# Project Setup & Workflow

Built in Unity 6.3 LTS (Universal Render Pipeline).

## Tech Stack

- Unity 6.3 LTS — any patch version within 6.3.x is fine; do not use a different major/minor version (e.g. 6.2 or 6.4), as this can cause scene/asset corruption or forced reimports.
- Git + Git LFS for version control.

## Getting Started

1. Install Git if you don't already have it: download from [git-scm.com](https://git-scm.com/downloads) and run the installer, keeping the default options.
2. Install Git LFS if you haven't already. Open a terminal (Git Bash, or PowerShell on Windows) and run:

```
   git lfs install
```

   You only need to do this once per device, ever — not once per project. If you run it again by accident later, nothing breaks.

3. Choose a folder on your computer where you want the project to live (e.g. `D:\projects\`), then open a terminal in that folder and clone the repo:

```
   git clone https://github.com/erceniq/game-graduation-project.git
```

   This downloads the whole project into a new folder called `game-graduation-project` inside wherever you ran the command.

4. Open **Unity Hub** → click **Add** → **Add project from disk** → select the `game-graduation-project` folder you just cloned. Make sure it opens with Unity **6.3 LTS** (any patch version).
5. Once the project is open in Unity, confirm these Editor settings (only needs to be done once, right after cloning):
   - Set **Version Control Mode** to **Visible Meta Files**
   - Set **Asset Serialization Mode** to **Force Text**

## Workflow Rules (Important — Read Before Committing)

`main` is protected. You cannot push directly to it. All changes go through a Pull Request. Follow these steps every time you start new work:

1. Open a terminal inside your project folder (right-click inside the folder → "Open in Terminal" or "Git Bash Here", depending on what's installed).
2. Make sure you're up to date with the latest version before starting:

```
   git checkout main
   git pull origin main
```

3. Create your own branch to work on. Replace `branch-name` with something like `player-movement`:

```
   git checkout -b branch-name
```

   This creates a new branch and switches you onto it. You are now safely working on your own copy — you cannot break anyone else's work from here.

4. Make your changes in Unity as normal (add assets, write code, edit scenes, etc.).
5. Once you're ready to save your progress, go back to the terminal and run these three commands in order:

```
   git add .
```

   This stages all your changed files — tells Git "include these in the save."

```
   git commit -m "short description of what you did"
```

   This actually saves a snapshot of your changes with a short message explaining what you did. Replace the text in quotes with something meaningful, e.g. `"add player jump animation"`.

```
   git push -u origin branch-name
```

   This uploads your branch and its changes to GitHub, so others (and you, from another computer) can see it.

6. Go to the repo page on GitHub.com in your browser. You should see a banner near the top saying your branch was recently pushed, with a button labeled **"Compare & pull request"** — click it.
7. On the next page, you can add a short description if you want, then click the green **Create pull request** button.
8. Once your Pull Request is reviewed and approved, click **Merge pull request**, then **Confirm merge**. Your changes are now part of `main`.
9. Back in your terminal, switch back to `main` and pull the latest version so your local copy includes your newly merged work too:

```
   git checkout main
   git pull origin main
```

10. Your feature branch has now been merged and is no longer needed. Clean it up so your branch list doesn't get cluttered over time.

   Delete it locally:

```
    git branch -d branch-name
```

   Delete it from GitHub too, either use the command below or delete from GitHub web page:

```
    git push origin --delete branch-name
```

   Replace `branch-name` with the actual visible branch name in GitHub.

**If your branch takes more than a day or two to finish:** periodically pull the latest `main` into your branch so you don't drift too far apart and end up with a painful merge conflict later:

```
git checkout branch-name
git pull origin main
```

## Working with Scenes and Prefabs (Avoiding Conflicts)

Scenes and prefabs are binary files and cannot be merged if two people edit them at the same time. Locking a file tells everyone else on the team, across all branches, "I'm working on this right now, don't push changes to it."

**Important things to understand about locking:**

- Locking happens immediately on GitHub the moment you run the command — it is not something you need to push separately.
- Locking only actually blocks a push if the file's path is marked `lockable` in `.gitattributes` (already set up in this repo for scenes and prefabs). Without that, `git lfs lock` still records who has it "checked out," but won't stop someone else from pushing changes to it — so don't skip locking just because a file doesn't visibly change.
- Locks apply to the entire repository, not just one branch. Even if a teammate is working on a completely different branch, they still cannot push changes to a file you've locked.
- Always check for existing locks before starting work on a scene or prefab, and always unlock when you're done, so you don't block your teammates unnecessarily.

**Before editing a scene or prefab**, check nobody else already has it locked:

```
git lfs locks
```

If it's free, lock it:

```
git lfs lock Assets/Scenes/YourScene.unity
```

**When you're done and have pushed your changes**, unlock it so others can work on it:

```
git lfs unlock Assets/Scenes/YourScene.unity
```

**To check which files are currently locked and by whom, at any time:**

```
git lfs locks
```

## License

All rights reserved. See [LICENSE](./LICENSE) for details.