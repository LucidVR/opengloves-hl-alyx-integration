<script>
    import {FontAwesomeIcon} from 'fontawesome-svelte';
    import {faQuestionCircle} from '@fortawesome/free-solid-svg-icons';

    import ToolTip from './components/ToolTip.svelte';

    import {getLocalStorageKey, setLocalStorageKey} from "./utils/localStorage";
    import Input from "./components/Input.svelte";
    import Button from "./components/Button/DefaultButton.svelte";

    import ToastStore from './stores/toast';

    let hlAlyxPath = getLocalStorageKey('hl-root-path');

    const onSubmit = () => {
        console.log(hlAlyxPath);
        setLocalStorageKey('hl-root-path', hlAlyxPath);
        ToastStore.addToast(ToastStore.severity.SUCCESS, 'Success saving settings');
    }
</script>
<div class="w-full flex flex-col justify-center items-center">
    <div class="mx-10 w-full overflow-auto">
        <h2 class="mb-5 text-center text-3xl font-extrabold text-gray-900">
            Configure Integration Settings
        </h2>
    </div>
    <div class="flex flex-row justify-center items-center w-full">
        <Input bind:value={hlAlyxPath} placeholder="E:\SteamLibrary\steamapps\common\Half-Life Alyx"/>
        <ToolTip title="This is the file location to the Half-Life: Alyx root folder path">
            <div class="p-1">
                <FontAwesomeIcon icon={faQuestionCircle} size="2x"/>
            </div>
        </ToolTip>
    </div>
    <div class="flex flex-row px-4 py-3">
        <div class="flex-grow"></div>
        <Button onClick={onSubmit}>Save</Button>
    </div>
</div>