(function ($) {
    'use strict';
    const editor = ace.edit('aceExample');
    editor.setTheme('ace/theme/chaos');
    editor.getSession().setMode('ace/mode/javascript');
    document.getElementById('aceExample').style.fontSize = '1rem';
})(jQuery);
//# sourceMappingURL=ace.js.map