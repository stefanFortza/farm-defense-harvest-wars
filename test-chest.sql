-- Run this SQL against your AspNetUsers table to grant yourself a test chest.
-- Replace 'your@email.com' with the actual account email you are using.

UPDATE AspNetUsers 
SET ChestsJson = '[{"Id":"test-chest-1","Type":0,"Rarity":0,"RemainingSeconds":0,"IsOpening":false}]'
WHERE Email = 'your@email.com';

-- Chest Types: 0=Wood, 1=Silver, 2=Gold
-- Rarity: 0=Common, 1=Rare, 2=Epic
-- RemainingSeconds: 0 means ready to open instantly.
