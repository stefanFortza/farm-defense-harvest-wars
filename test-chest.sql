-- Run this SQL against your AspNetUsers table to grant yourself a test chest.
-- Replace 'stefan@test.com' with the actual account email you are using.

UPDATE AspNetUsers 
SET ChestsJson = '[{"Id":"test-wooden","Name":"Wooden Chest","AcquiredAt":"2026-04-28T10:00:00Z","UnlockDurationSeconds":10},{"Id":"test-silver","Name":"Silver Chest","AcquiredAt":"2026-04-28T10:00:00Z","UnlockDurationSeconds":60},{"Id":"test-gold","Name":"Golden Chest","AcquiredAt":"2026-04-28T10:00:00Z","UnlockDurationSeconds":120}]'
WHERE Email = 'stefan@test.com';

-- Note: The JSON structure matches ChestDto.cs
-- Id: Unique identifier
-- Name: "Wooden Chest", "Silver Chest", or "Golden Chest" (determines texture)
-- AcquiredAt: ISO Date
-- UnlockDurationSeconds: 10 (Wood), 60 (Silver), 120 (Gold)
-- UnlockStartTime: Null means not started.
