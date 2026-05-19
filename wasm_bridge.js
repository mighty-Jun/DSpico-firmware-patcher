let ndstoolInstance = null;
let dldipatchInstance = null;

window.NdsToolBridge = {
    initNdstool: async function() {
        if (!ndstoolInstance) {
            console.log("[ndstool] WASM engine initializing...");
            ndstoolInstance = await createNdstool();
        }
        return ndstoolInstance;
    },

    extractBanner: async function (fileName, romBytes) {
        const engine = await this.initNdstool();
        try {
            engine.FS.writeFile(fileName, new Uint8Array(romBytes));
            engine.callMain(['-x', fileName, '-t', 'banner.bin']);

            let bannerData = engine.FS.readFile('banner.bin');
            console.log(`[ndstool] banner extracted! (${bannerData.length} bytes)`);

            return bannerData;
        } catch (error) {
            console.error("[ndstool] banner extraction failed:", error);
            return null;
        }
    },

    buildBootloader: async function (bannerBytes, arm7Bytes, arm9Bytes) {
        const engine = await this.initNdstool();
        try {
            engine.FS.writeFile('banner.bin', new Uint8Array(bannerBytes));
            engine.FS.writeFile('arm7.elf', new Uint8Array(arm7Bytes));
            engine.FS.writeFile('arm9.elf', new Uint8Array(arm9Bytes));

            engine.callMain(['-c', 'bootloader.nds', '-9', 'arm9.elf', '-7', 'arm7.elf', '-t', 'banner.bin', '-n', '1623', '1', '-n', '2296', '24', '-g', 'DSPI']);
            //-n 1623 1 -n1 2296 24 -g DSPI
            let bootloaderData = engine.FS.readFile('bootloader.nds');
            console.log(`[ndstool] bootloader built! (${bootloaderData.length} bytes)`);

            return bootloaderData;
        } catch (error) {
            console.error("[ndstool] bootloader building failed:", error);
            return null;
        }
    },

    buildBootloaderFromIcon: async function (title, subtitle, author, iconBytes, arm7Bytes, arm9Bytes) {
        const engine = await this.initNdstool();
        try {
            engine.FS.writeFile('icon.bmp', new Uint8Array(iconBytes));
            engine.FS.writeFile('arm7.elf', new Uint8Array(arm7Bytes));
            engine.FS.writeFile('arm9.elf', new Uint8Array(arm9Bytes));

            fullTitle = [title, subtitle, author].filter(item => item !== "").join(';');
            
            engine.callMain(['-c', 'bootloader.nds', '-9', 'arm9.elf', '-7', 'arm7.elf', '-b', 'icon.bmp', fullTitle, '-n', '1623', '1', '-n', '2296', '24', '-g', 'DSPI']);
            //-n 1623 1 -n1 2296 24 -g DSPI
            let bootloaderData = engine.FS.readFile('bootloader.nds');
            console.log(`[ndstool] bootloader built! (${bootloaderData.length} bytes)`);

            return bootloaderData;
        } catch (error) {
            console.error("[ndstool] bootloader building failed:", error);
            return null;
        }
    }
};

window.DldiBridge = {
    initDldipatch: async function() {
        if (!dldipatchInstance) {
            console.log("[dldipatch] WASM engine initializing...");
            dldipatchInstance = await createDldipatch();
        }
        return dldipatchInstance;
    },

    applyPatch: async function (dldiBytes, bootloaderBytes) {
        const engine = await this.initDldipatch();
        try {
            engine.FS.writeFile('dspico.dldi', new Uint8Array(dldiBytes));
            engine.FS.writeFile('bootloader.nds', new Uint8Array(bootloaderBytes));

            engine.callMain(['patch', 'dspico.dldi', 'bootloader.nds']);

            let patchedData = engine.FS.readFile('bootloader.nds');

            return patchedData;
        } catch (error) {
            console.error("[dldipatch] DLDI patching failed:", error);
            return null;
        }
    }
};