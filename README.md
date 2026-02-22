# MaxUpton_CrimsonCode2026

This solution contains three projects:

* **QuantumTicTacToe.Engine** – a class library with all of the game logic
  (board, moves, cycle detection, collapse, win rules, history, etc.).
  Target framework is `netstandard2.1` so the compiled DLL can be consumed by
  other .NET hosts (including Unity).

* **QuantumTicTacToe** – a WinForms application that references the engine
  library and provides a simple desktop UI. It targets `net7.0-windows`.

* **QuantumTicTacToe** - a Unity application that references the engine
  library and provides a more advanced game UI.

The unity project is the culmination of my work on the other two 
and what I am submitting for judgment at the 2026 crimson code hackathon


