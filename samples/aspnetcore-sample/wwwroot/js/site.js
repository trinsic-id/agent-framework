// Write your JavaScript code.

function copyToCliboard(element) {
    /* Select the text field */
    element.select();

    /* Copy the text inside the text field */
    document.execCommand("copy");
} 