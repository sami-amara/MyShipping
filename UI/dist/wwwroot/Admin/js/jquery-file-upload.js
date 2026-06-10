(function ($) {
    'use strict';
    if ($('#fileuploader').length) {
        $('#fileuploader').uploadFile({
            url: '../../../assets/images/',
            fileName: 'myfile'
        });
    }
})(jQuery);
//# sourceMappingURL=jquery-file-upload.js.map