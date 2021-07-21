<script>
    export let checked = false;
</script>

<div>
    <label class="inline-flex items-center">
        <input type="checkbox" class="form-checkbox h-6 w-6" bind:checked>
        <span class="ml-2"><slot /></span>
    </label>
</div>