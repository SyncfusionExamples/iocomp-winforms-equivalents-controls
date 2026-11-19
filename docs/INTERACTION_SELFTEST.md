# Interaction self-test

Synthetic input: the controls' own mouse/keyboard handlers are called with scripted event args on a hidden form.
It proves state changes and events are wired; it does not prove how the controls feel with a real mouse.

Result: 15 of 15 passed.

| Control | Check | Result | Detail |
|---|---|---|---|
| Knob (stock renderer, control case) | drag from 9 to 3 o'clock raises the value and fires ValueChanged | PASS | ok |
| Knob | drag from 9 to 3 o'clock raises the value and fires ValueChanged | PASS | ok |
| SwitchToggle | click toggles Active/Inactive and fires ToggleStateChanged | PASS | ok |
| SwitchLed | click toggles Active/Inactive and fires ToggleStateChanged | PASS | ok |
| SwitchRocker | click toggles Active/Inactive and fires ToggleStateChanged | PASS | ok |
| SwitchRocker3Way | top half steps up, bottom half steps down, arrows work | PASS | ok |
| SwitchLever | click toggles Checked and fires CheckedChanged | PASS | ok |
| SwitchRotary | click on a position label selects it; arrow keys move | PASS | ok |
| SwitchQuad | pressing each arm selects Up/Right/Down/Left | PASS | ok |
| SwitchPanel | clicking each label selects it | PASS | ok |
| SwitchSlider | clicking near a position snaps to it; keys move | PASS | ok |
| Led | click toggles when Interactive and fires IsOnChanged | PASS | ok |
| MatrixLeds | click toggles the cell under the mouse | PASS | ok |
| Valve | click and Space toggle IsOpen | PASS | ok |
| Motor | Running toggles and fires RunningChanged | PASS | ok |
