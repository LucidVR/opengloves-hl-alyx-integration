<script>
    import {Command} from '@tauri-apps/api/shell';

    import {FontAwesomeIcon} from 'fontawesome-svelte';
    import {faQuestionCircle} from '@fortawesome/free-solid-svg-icons';

    import ToolTip from './components/ToolTip.svelte';

    import {getLocalStorageKey, setLocalStorageKey} from "./utils/localStorage";
    import Input from "./components/Input.svelte";

    import ToastStore from './stores/toast';
    import DefaultButton from "./components/Button/DefaultButton.svelte";
    import OrangeButton from "./components/Button/OrangeButton.svelte";
    import Checkbox from "./components/Checkbox.svelte";
    import Log from "./components/Log.svelte";

    let path = getLocalStorageKey('hl-root-path');
    let invertHands = getLocalStorageKey('invert-hands');

    let isListening = false;
    let stdout = ["...\n"];

    const onSubmit = () => {
        setLocalStorageKey('hl-root-path', path);
        setLocalStorageKey('invert-hands', invertHands);

        ToastStore.addToast(ToastStore.severity.SUCCESS, 'Success saving settings.');
    }

    let sidecar;
    let child;

    const beginSidecar = async () => {
        let stdin = {
            path: (path + "/game/hlvr/console.log").replace(/\//g, '\\'),
            invertHands: invertHands === 'true'
        };

        sidecar = Command.sidecar('sidecar-filewatcher');
        sidecar.stdout.on('data', e => {
            stdout = [...stdout, e];
            console.log(e);
        });
        sidecar.stderr.on('data', e => {
            stdout = [...stdout, e];
            console.error(e);
        });

        child = await sidecar.spawn();
        await child.write(JSON.stringify(stdin).replace(/\\n/g, '') + "\n");

        isListening = true;
    };

    const stopListening = () => {
        stdout = ['...'];
        child.write("stop\n");
        isListening = false;
    };
</script>
<div class="w-full flex flex-col justify-center items-center">
    <div class="mx-10 w-full overflow-auto">
        <h2 class="mb-5 text-center text-3xl font-extrabold text-gray-900">
            Configure Integration Settings
        </h2>
    </div>
    <div class="flex flex-col justify-center items-center w-full">
        <div class="flex flex-row w-full">
            <Input bind:value={path} placeholder="E:\SteamLibrary\steamapps\common\Half-Life Alyx"/>
            <ToolTip title="This is the file location to the Half-Life: Alyx root folder path">
                <div class="p-1">
                    <FontAwesomeIcon icon={faQuestionCircle} size="2x"/>
                </div>
            </ToolTip>
        </div>

        <div class="flex flex-col py-5">
            <Checkbox bind:checked={invertHands}>Invert hands</Checkbox>
        </div>
    </div>
    <div class="flex flex-row px-4 py-3">
        <div class="flex-grow"></div>
        <DefaultButton onClick={onSubmit}>Save</DefaultButton>
    </div>
    <div class="flex flex-col py-3">
        {#if !isListening}
            <OrangeButton onClick={beginSidecar}>Begin Half-Life: Alyx integration!</OrangeButton>
        {:else}
            <Log logItems={stdout} />
            <DefaultButton onClick={stopListening}>Stop Listening</DefaultButton>
        {/if}
    </div>
</div>