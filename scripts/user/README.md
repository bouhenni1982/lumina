ضع هنا سكربتات Lua المخصصة للمستخدم بدون تعديل السكربتات الأصلية.

الترتيب المحلي داخل المشروع:

- `scripts/user/focus_profile.lua`
- `scripts/user/apps/<process>.lua`

مثال:

- `scripts/user/apps/chrome.lua`
- `scripts/user/apps/code.lua`

هذه الملفات تُحمَّل بعد السكربتات الأصلية، لذلك يمكنها override للسلوك الحالي مع الاحتفاظ بإمكانية الرجوع إلى `previous_handler` داخل Lua.
