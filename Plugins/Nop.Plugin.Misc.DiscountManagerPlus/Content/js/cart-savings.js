(function ($) {
    function openRewardPopup(popupSource) {
        if (!popupSource || !$.magnificPopup) {
            return;
        }

        $.magnificPopup.open({
            items: {
                src: popupSource,
                type: 'inline'
            },
            midClick: true,
            removalDelay: 150,
            mainClass: 'ns-dmp-mfp-wrap'
        });
    }

    function initRewardPopup($root) {
        if ($(document).data('nsDmpRewardPopupBound')) {
            return;
        }

        $(document).data('nsDmpRewardPopupBound', true);

        $(document).on('click.nsDmpRewardModal', '.js-ns-dmp-reward-open', function (event) {
            var popupSource = $(this).data('mfp-src');
            event.preventDefault();
            openRewardPopup(popupSource);
        });
    }

    function autoOpenRewardPopup($root, attempt) {
        var popupSource = $root.data('auto-open-popup');

        if (!popupSource || $root.data('reward-popup-opened')) {
            return;
        }

        if (!$.magnificPopup) {
            if (attempt >= 10) {
                return;
            }

            window.setTimeout(function () {
                autoOpenRewardPopup($root, attempt + 1);
            }, 200);
            return;
        }

        $root.data('reward-popup-opened', true);
        openRewardPopup(popupSource);
    }

    function syncRewardSelection($form) {
        if (!$form || $form.length === 0) {
            return;
        }

        var $selected = $form.find('.ns-dmp-reward-option__radio:checked').first();
        if ($selected.length === 0) {
            $selected = $form.find('.ns-dmp-reward-option__radio').first();
            if ($selected.length > 0) {
                $selected.prop('checked', true);
            }
        }

        $form.find('.ns-dmp-reward-option').removeClass('is-selected');
        if ($selected.length > 0) {
            $selected.closest('.ns-dmp-reward-option').addClass('is-selected');
        }

        var selectedId = $selected.val();
        $form.find('.ns-dmp-reward-attributes').addClass('is-hidden');
        if (selectedId) {
            $form.find('.ns-dmp-reward-attributes[data-option-id="' + selectedId + '"]').removeClass('is-hidden');
        }

        var hasSelection = $selected.length > 0;
        $form.find('.js-ns-dmp-reward-add').prop('disabled', !hasSelection);
    }

    function attachRewardSubmit($root) {
        if ($(document).data('nsDmpRewardSubmitBound')) {
            return;
        }

        $(document).data('nsDmpRewardSubmitBound', true);

        function submitRewardForm($form) {
            if (!$form || $form.length === 0) {
                return;
            }

            var $button = $form.find('.js-ns-dmp-reward-add').first();
            var $buttonText = $button.find('.ns-dmp-cart-reward-popup__submit-text');
            var $messageBox = $form.find('.ns-dmp-cart-reward-popup__message');
            var errorMessage = $root.data('reward-error') || 'Could not add the reward to the cart.';
            var selectionMessage = $form.data('selection-required') || 'Please select a reward item.';
            var defaultButtonText = $button.data('default-text') || $.trim($buttonText.text());
            var loadingText = $button.data('loading-text') || 'Adding...';

            if ($button.prop('disabled')) {
                return;
            }

            $button.data('default-text', defaultButtonText);
            syncRewardSelection($form);

            if ($form.find('.ns-dmp-reward-option__radio:checked').length === 0) {
                $messageBox.text(selectionMessage).removeClass('is-hidden');
                $button.prop('disabled', false).removeClass('is-loading');
                $buttonText.text(defaultButtonText);
                return;
            }
            $messageBox.addClass('is-hidden').text('');
            $button.prop('disabled', true).addClass('is-loading');
            $buttonText.text(loadingText);

            var payload = $form.serialize();

            $.ajax({
                cache: false,
                url: $form.data('action'),
                type: 'POST',
                data: payload,
                success: function (data) {
                    if (data && data.success) {
                        window.location.reload();
                        return;
                    }

                    if (data && data.message) {
                        $messageBox.text(data.message).removeClass('is-hidden');
                    }

                    $button.prop('disabled', false).removeClass('is-loading');
                    $buttonText.text(defaultButtonText);
                },
                error: function () {
                    $messageBox.text(errorMessage).removeClass('is-hidden');
                    $button.prop('disabled', false).removeClass('is-loading');
                    $buttonText.text(defaultButtonText);
                }
            });
        }

        $(document).on('submit.nsDmpRewardSubmit', '.ns-dmp-cart-reward-popup__form', function (event) {
            event.preventDefault();
            submitRewardForm($(this));
        });

        $(document).on('click.nsDmpRewardSubmit', '.js-ns-dmp-reward-add', function (event) {
            event.preventDefault();
            submitRewardForm($(this).closest('.ns-dmp-cart-reward-popup__form'));
        });

        $(document).on('change.nsDmpRewardOption', '.ns-dmp-reward-option__radio', function () {
            var $form = $(this).closest('.ns-dmp-cart-reward-popup__form');
            syncRewardSelection($form);
        });

        $(document).on('click.nsDmpRewardOption', '.ns-dmp-reward-option', function (event) {
            if ($(event.target).is('input, label, a, button, select, textarea')) {
                return;
            }

            var $radio = $(this).find('.ns-dmp-reward-option__radio');
            if ($radio.length > 0) {
                $radio.prop('checked', true).trigger('change');
            }
        });
    }

    $(function () {
        $('.ns-dmp-cart-savings-root').each(function () {
            var $root = $(this);
            initRewardPopup($root);
            attachRewardSubmit($root);
            $root.find('.ns-dmp-cart-reward-popup__form').each(function () {
                syncRewardSelection($(this));
            });
            autoOpenRewardPopup($root, 0);
        });
    });
})(jQuery);
