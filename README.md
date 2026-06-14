# LECIA.Enhanced

LECIA Enables Class Island Anywhere!

将 ClassIsland 的课表信息通过串口（COM）或 UDP 发送到外部设备（如单片机、ESP32 等），适合桌搭、电子课表牌等硬件项目。

此项目仅兼容 ClassIsland 2.0.0.0 及以上版本。

---

## 1. 通讯模式

| datatarget | 传输方式 | 数据格式 | 说明 |
|------------|----------|----------|------|
| `0` | COM 串口 | 文本（关键字） | 通过串口发送自定义格式文本，末尾追加 `\n` |
| `1` | UDP | 文本（关键字） | 通过 UDP 发送自定义格式文本 |
| `2` | COM 串口 | JSON | 通过串口发送完整课表 JSON，末尾追加 `\n` |
| `3` | UDP | JSON | 通过 UDP 发送完整课表 JSON |

---

## 2. 文本格式模式 (datatarget = 0 / 1)

### 2.1 关键字

> ⚠️ 关键字**大小写敏感**

| 关键字 | 释义 |
|--------|------|
| `{NextPointTime}` | 距离下个时间点的剩余时间 |
| `{ClassLeftTime}` | 距离上课的剩余时间 |
| `{BreakingLeftTime}` | 距离下课的剩余时间 |
| `{CurrentSubjectName}` | 当前课程名（无课程时显示"无课程"） |
| `{CurrentClassPlan}` | 当前课表（科目名逗号分隔） |

### 2.2 示例

**格式字符串：**
```
NextPointTime:{NextPointTime} ClassLeftTime:{ClassLeftTime} BreakingLeftTime:{BreakingLeftTime} Subject:{CurrentSubjectName} ClassPlan:{CurrentClassPlan}
```

**实际输出：**
```
NextPointTime:00:15:10.3 ClassLeftTime:00:00:00 BreakingLeftTime:00:15:10.3 Subject:数学 ClassPlan:语文,语文,数学,数学,英语,英语,政治
```

---

## 3. JSON 格式模式 (datatarget = 2 / 3)

发送完整的当日课表数据，包含所有科目、时间表和本周课程安排。

### 3.1 结构

```json
{
  "days": [{
    "weekday": 1,
    "timetable_id": 0,
    "classes": [{"id": 0}, {"id": 1}]
  }],
  "subjects": [{
    "id": 0,
    "name": "数学",
    "short_name": "数",
    "teacher": "张三",
    "is_outside": false
  }],
  "timetables": [{
    "id": 0,
    "name": "默认时间表",
    "classes": [
      {
        "id": 0,
        "name": "第一节",
        "start": 1718316000,
        "end": 1718319600,
        "teacher": null,
        "is_outside": false
      },
      {
        "is_break": true,
        "name": "课间休息",
        "start": 1718319600,
        "end": 1718319900
      }
    ]
  }]
}
```

### 3.2 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `days[].weekday` | int | 星期几（0=周日, 1=周一 ... 6=周六） |
| `days[].timetable_id` | int | 使用的时间表 ID，对应 `timetables[].id` |
| `days[].classes[].id` | int | 科目 ID，对应 `subjects[].id` |
| `days[].extended_layer` | object? | 覆盖层课表信息（仅在调课时出现） |
| `subjects[].id` | int | 科目唯一 ID（插件内部分配） |
| `subjects[].name` | string | 科目全名 |
| `subjects[].short_name` | string | 科目简称（`Initial`） |
| `subjects[].teacher` | string? | 授课老师 |
| `subjects[].is_outside` | bool | 是否为室外课程 |
| `timetables[].id` | int | 时间表唯一 ID |
| `timetables[].name` | string | 时间表名称 |
| `timetables[].classes[].id` | int | 时间段序号 |
| `timetables[].classes[].name` | string | 时间段名称（如"第一节"） |
| `timetables[].classes[].start` | long | 开始时间（Unix 时间戳，基于当日日期） |
| `timetables[].classes[].end` | long | 结束时间（Unix 时间戳，基于当日日期） |
| `timetables[].classes[].is_break` | bool? | `true` 表示课间休息 |
| `timetables[].classes[].teacher` | string? | 该时间段默认科目老师 |
| `timetables[].classes[].is_outside` | bool | 该时间段是否为室外 |

---

## 4. 配置文件

配置文件位置：`ClassIsland 安装目录\Config\Plugins\LECIA\config.ini`

```ini
[mainconfig]
autostart=1
datatarget=2
comport=COM6
baundrate=115200
maindataformat=NextPointTime:{NextPointTime} Subject:{CurrentSubjectName}
delay=200
udpnetIP=192.168.1.100
udpnetport=12345
```

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `autostart` | 启动 ClassIsland 时自动启用插件（0=否 1=是） | `0` |
| `datatarget` | 数据目标：0=COM文本 1=UDP文本 2=COM-JSON 3=UDP-JSON | `0` |
| `comport` | 串口号 | `COM1` |
| `baundrate` | 串口波特率 | `115200` |
| `maindataformat` | 文本模式格式串（JSON 模式下忽略） | _空_ |
| `delay` | 发送间隔（毫秒） | `200` |
| `udpnetIP` | UDP 目标 IP 地址 | _空_ |
| `udpnetport` | UDP 目标端口 | `12345` |

---

## 5. 注意事项

- 配置文件使用 INI 格式存储，由插件自动管理
- 遇到问题请在 [GitHub Issues](https://github.com/Aether-254/LECIA.Enhanced/issues) 提交
