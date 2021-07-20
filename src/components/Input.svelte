<script>
    export let value = '';
    export let placeholder = '';
</script>

<label class="block uppercase tracking-wide text-gray-700 text-xs font-bold mb-2" for="grid-last-name">
    <slot/>
</label>
<div class="flex flex-row w-full">
    <div class="w-full flex-grow">
        <input class="appearance-none block w-full bg-gray-200 text-gray-700 border border-gray-200 rounded py-3 px-4 leading-tight focus:outline-none focus:bg-white focus:border-gray-500"
               id="grid-last-name" type="text" placeholder={placeholder} bind:value>
    </div>
</div>