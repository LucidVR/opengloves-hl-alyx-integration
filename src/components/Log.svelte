<script>
    export let logItems = [];
    $: logString = logItems.reduce((acc, cur) => acc + cur);
</script>

<textarea class="w-full px-3 py-2 text-gray-700 border rounded-lg focus:outline-none" disabled value={logString} rows={logItems.length} cols="50"></textarea>