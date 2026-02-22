# MaxUpton_CrimsonCode2026

This solution contains two projects:

* **QuantumTicTacToe.Engine** – a class library with all of the game logic
  (board, moves, cycle detection, collapse, win rules, history, etc.).
  Target framework is `netstandard2.1` so the compiled DLL can be consumed by
  other .NET hosts (including Unity).

* **QuantumTicTacToe** – a WinForms application that references the engine
  library and provides a simple desktop UI. It targets `net7.0-windows`.

## Building

Run `dotnet build` at the solution root to compile both projects. The engine
assembly will be produced in `Engine/bin/Debug/netstandard2.1`.

## Unity integration

To port the game into Unity, copy `QuantumTicTacToe.Engine.dll` from the
engine project's output into your Unity project's `Assets/Plugins` folder.
Then add `using QuantumTicTacToe;` in your scripts and invoke the public API.

The WinForms code is intentionally isolated in the executable project and is
**not** included in the library, so it will have no effect in Unity.
