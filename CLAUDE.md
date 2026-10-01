# Working on this repo

## The default branch is the release

The owner installs and updates the SloppyMods mods straight from GitHub: RimSort
clones this repository's default branch (`main`) into RimWorld's Mods
folder and pulls it in place, so **every commit pushed to `main` is what the
game loads next**.

- Push finished work to `main`. Nothing half-done: each commit must load
  and play as-is.
- **Snapshot every build on its own branch.** Every commit carries a build
  number (`0.9.<commit count>`, stamped below). Push the same commit to
  `main` AND to a branch named after that build, so any build can be
  restored:

      git push origin HEAD:main HEAD:refs/heads/build/0.9.N

  Build branches are restore points: never move or delete one. To roll back,
  reset `main` to an earlier `build/...` branch - only when the owner asks.
- **Stamp the build number before every commit**: `<modVersion>` in
  `About/About.xml` and a `Build x.y` line at the top of its description, set to
  `0.9.<commit count after this commit>` (`git rev-list --count HEAD` + 1; needs
  a full, not shallow, clone). The build branch uses the same number.
- **Commit the compiled assemblies** in the same commit as any C# change:
  RimWorld loads the DLL, not the source.
- Keep `packageId` unchanged: saves and RimSort key on it. The display
  `<name>` is "SloppyMod Trash POWER!".

## Standing preferences

- For art changes, show the owner before/after comparisons - but they ship to
  the default branch like any other change; the build branch is the way back.
