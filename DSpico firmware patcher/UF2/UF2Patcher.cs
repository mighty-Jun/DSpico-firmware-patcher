namespace DSpico_firmware_patcher.UF2
{
    public class UF2Patcher
    {
        private readonly UF2Reader _uf2Reader;
        private readonly byte[] _dummyPattern = "DUMMYBEGIN"u8.ToArray();
        public UF2Patcher(UF2Reader uf2Reader)
        {
            _uf2Reader = uf2Reader;
        }

        public void PatchDefault(byte[] ndsRom)
        {
            var currentBlockOffset = 0;
            int currentBlockIndex = 0;
            var currentBlock = _uf2Reader.Blocks[0];

            for (var i = 0; i < _uf2Reader.Blocks.Count; i++)
            {
                currentBlock = _uf2Reader.Blocks[i];
                currentBlockOffset = GetBlockPatternOffset(currentBlock);

                if (currentBlockOffset >= 0)
                {
                    currentBlockIndex = i;
                    break;
                }
            }

            if (currentBlockOffset == -1)
            {
                throw new Exception("Could not find dummy pattern.");
            }

            var romOffset = 0;

            while (romOffset < ndsRom.Length)
            {
                if (currentBlockOffset >= currentBlock.DataSize)
                {
                    currentBlockIndex++;

                    if (currentBlockIndex >= _uf2Reader.Blocks.Count)
                    {
                        throw new Exception($"UF2 base firmware has insufficient dummy space! (Patch ROM size: {ndsRom.Length} bytes)");
                    }

                    currentBlock = _uf2Reader.Blocks[currentBlockIndex];
                    currentBlockOffset = 0;
                }

                currentBlock.Data[currentBlockOffset] = ndsRom[romOffset];

                ++currentBlockOffset;
                ++romOffset;
            }
        }

        public void PatchWRFU(byte[] wrfuRom)
        {
            var currentBlockOffset = 0;
            var currentBlock = _uf2Reader.Blocks[0];

            // Find dummy pattern
            for (var i = 0; i < _uf2Reader.Blocks.Count; i++)
            {
                currentBlock = _uf2Reader.Blocks[i];

                currentBlockOffset = GetBlockPatternOffset(currentBlock);

                if (currentBlockOffset >= 0)
                {
                    break;
                }
            }

            if (currentBlockOffset == -1)
            {
                throw new Exception("Could not find dummy pattern");
            }
            
            var wrfuOffset = 0;

            //Now start replacing dummy with WRFU
            while (wrfuOffset < wrfuRom.Length)
            {
                if (currentBlockOffset >= currentBlock.DataSize)
                {
                    currentBlock = _uf2Reader.Blocks.First(x => x.Address == currentBlock.Address + currentBlock.DataSize);
                    currentBlockOffset = 0;
                }

                currentBlock.Data[currentBlockOffset] = wrfuRom[wrfuOffset];

                ++currentBlockOffset;
                ++wrfuOffset;
            }
        }

        private int GetBlockPatternOffset(UF2Block block)
        {
            for (var i = 0; i < block.DataSize; i++)
            {
                bool isMatch = true;

                for (var j = 0; j < _dummyPattern.Length; j++)
                {
                    int currentOffset = i + j;

                    if (currentOffset < block.DataSize)
                    {
                        if (block.Data[currentOffset] != _dummyPattern[j])
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    else
                    {
                        var nextBlock = _uf2Reader.Blocks.FirstOrDefault(x => x.Address == block.Address + block.DataSize);

                        if (nextBlock == null || nextBlock.Data[currentOffset - block.DataSize] != _dummyPattern[j])
                        {
                            isMatch = false;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool BlockStartsWithPattern(UF2Block block, int patternOffset)
        {
            if (patternOffset >= _dummyPattern.Length)
            {
                return false;
            }

            var remaining = _dummyPattern.Length - patternOffset;

            if (block.DataSize < remaining)
            {
                return false;
            }

            for (var i = patternOffset; i < _dummyPattern.Length; i++)
            {
                if (block.Data[i] != _dummyPattern[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
