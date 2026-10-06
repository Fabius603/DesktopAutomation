# Macro commands and editor

The macro command catalog, key aliases, combination validation and scheduled command durations
are owned by `TaskAutomation/Makros/MakroCommandRules.cs`. Execution, recording conversion,
validation and UI projections use this shared owner.

`TextInputBefehl` (`text_input`) sends Unicode text; `KeyCombinationBefehl` (`key_combination`)
presses ordered modifiers and a main key, then releases keys it owns in reverse order.
Cancellation and input failures release owned keys without releasing a modifier already held
by another macro command. Recorded composite commands retain their elapsed duration in
`durationUs`, in addition to the existing precise delay before the command.

The additions use the existing polymorphic JSON format. Old recordings remain raw by default;
`combineKeyboardInputs` is opt-in. The recording mapper combines only adjacent, complete text
key pairs or complete modifier/main-key sequences. Interrupted sequences remain raw. Text
translation uses the foreground keyboard layout without changing its dead-key state; modified,
dead-key and IME input remains raw when its meaning cannot be established safely. The
translation does not promise reconstruction of arbitrary composed text or held-key repeats.

The WPF editor retains independent drafts per command. Applying or saving validates drafts
before replacing commands, preserves IDs and group membership, and creates an undo snapshot.
Invalid numeric input remains visible and blocks saving. Real execution is unavailable while
there are unsaved drafts, a recording, or an active visual preview. Preview uses the existing
screen overlay without sending input, with seeking, speed changes and automatic completion.

`MacroStepFields` is the shared form for adding commands and editing selected commands.
The native rendering fixture exercises actual WPF bindings in German and English, light and
dark themes, standard and compact windows, as well as the actual overlay playback clock.
