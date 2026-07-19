仓库零件管理系统源文件说明

一、文件结构
1. database/warehouse.sql：数据库创建脚本、parts 表结构和测试数据。
2. php/config.php：数据库连接配置文件。
3. php/index.php：零件列表与查询页面。
4. php/part_add.php：零件信息添加页面。
5. php/part_edit.php：零件信息修改页面。
6. php/part_delete.php：零件信息删除页面。
7. php/part_view.php：零件信息查看页面。
8. php/style.css：页面样式文件。

二、数据库说明
数据库名称：warehouse_db
数据表名称：parts
主码：part_no
兼容版本：MySQL 5.5
字符集：utf8

三、运行说明
1. 在 MySQL 中执行 database/warehouse.sql，创建数据库、数据表并插入测试数据。
2. 将 php 文件夹放入支持 PHP 和 MySQL 的 Web 服务器目录。
3. 按实际数据库账号修改 php/config.php 中的用户名、密码。
4. 通过浏览器访问 php/index.php。

四、功能说明
系统实现零件信息的添加、修改、删除和查看功能，同时支持按零件号、零件名、颜色进行简单查询。
