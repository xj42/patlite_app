use crate::discovery::{discover_devices, DiscoveredDevice, DiscoveryConfig};
use crate::protocol::{BuzzerPattern, Flash, La6Colour, PatlitePattern, PRESETS};
use crate::transport::{parse_port, send_pattern, validate_endpoint, DEFAULT_COMMAND_PORT};
use iced::widget::{
    button, column, container, pick_list, row, scrollable, text, text_input, Space,
};
use iced::{application, Background, Border, Color, Element, Length, Size, Task, Theme};

const DEFAULT_IP: &str = "127.0.0.1";
const DEFAULT_PORT: &str = "12000";
const MAX_LOG_LINES: usize = 200;

pub fn run() -> iced::Result {
    application(PatliteApp::default, PatliteApp::update, PatliteApp::view)
        .title(app_title)
        .theme(app_theme)
        .window_size(Size::new(900.0, 620.0))
        .centered()
        .run()
}

fn app_title(_: &PatliteApp) -> String {
    "Patlite Controller (Rust)".to_string()
}

fn app_theme(_: &PatliteApp) -> Theme {
    Theme::Light
}

#[derive(Debug, Clone)]
pub enum Message {
    IpChanged(String),
    PortChanged(String),
    TierChanged(usize, La6Colour),
    FlashChanged(Flash),
    BuzzerChanged(BuzzerPattern),
    SendPressed,
    OffPressed,
    PresetPressed(usize),
    SendFinished(Result<PatlitePattern, String>),
    DiscoverPressed,
    DiscoveryFinished(Vec<DiscoveredDevice>),
    DeviceSelected(usize),
}

#[derive(Debug, Clone)]
pub struct PatliteApp {
    ip: String,
    port: String,
    tiers: [La6Colour; 5],
    flash: Flash,
    buzzer: BuzzerPattern,
    applied: PatlitePattern,
    logs: Vec<String>,
    is_sending: bool,
    is_discovering: bool,
    discovered_devices: Vec<DiscoveredDevice>,
}

impl Default for PatliteApp {
    fn default() -> Self {
        Self {
            ip: DEFAULT_IP.to_string(),
            port: DEFAULT_PORT.to_string(),
            tiers: [
                La6Colour::Red,
                La6Colour::Off,
                La6Colour::Off,
                La6Colour::Off,
                La6Colour::Off,
            ],
            flash: Flash::Off,
            buzzer: BuzzerPattern::Off,
            applied: PatlitePattern::off(),
            logs: vec!["Rust controller ready.".to_string()],
            is_sending: false,
            is_discovering: false,
            discovered_devices: Vec::new(),
        }
    }
}

impl PatliteApp {
    pub fn update(&mut self, message: Message) -> Task<Message> {
        match message {
            Message::IpChanged(value) => {
                self.ip = value;
                Task::none()
            }
            Message::PortChanged(value) => {
                self.port = value;
                Task::none()
            }
            Message::TierChanged(index, value) => {
                if let Some(tier) = self.tiers.get_mut(index) {
                    *tier = value;
                }
                Task::none()
            }
            Message::FlashChanged(value) => {
                self.flash = value;
                Task::none()
            }
            Message::BuzzerChanged(value) => {
                self.buzzer = value;
                Task::none()
            }
            Message::SendPressed => self.start_send(self.current_pattern(), "custom pattern"),
            Message::OffPressed => self.start_send(PatlitePattern::off(), "Off"),
            Message::PresetPressed(index) => {
                if let Some(preset) = PRESETS.get(index) {
                    self.start_send(preset.pattern, preset.name)
                } else {
                    Task::none()
                }
            }
            Message::SendFinished(result) => {
                self.is_sending = false;
                match result {
                    Ok(pattern) => {
                        self.applied = pattern;
                        self.log("Send completed successfully.");
                    }
                    Err(error) => self.log(format!("Send failed: {error}")),
                }
                Task::none()
            }
            Message::DiscoverPressed => self.start_discovery(),
            Message::DiscoveryFinished(devices) => {
                self.is_discovering = false;
                self.discovered_devices = devices;
                let count = self.discovered_devices.len();
                if count == 0 {
                    self.log("Discovery completed: no Patlite devices found.");
                } else {
                    self.log(format!("Discovery completed: found {count} device(s)."));
                }
                Task::none()
            }
            Message::DeviceSelected(index) => {
                if let Some(device) = self.discovered_devices.get(index).cloned() {
                    self.ip = device.host.clone();
                    self.port = device.command_port.to_string();
                    self.log(format!("Selected discovered device {}.", device.label()));
                }
                Task::none()
            }
        }
    }

    pub fn view(&self) -> Element<'_, Message> {
        let busy = self.is_busy();
        let can_send = self.can_send();

        let endpoint = row![
            text("IP").width(25),
            text_input("127.0.0.1", &self.ip)
                .on_input(Message::IpChanged)
                .width(150),
            text("Port").width(35),
            text_input("12000", &self.port)
                .on_input(Message::PortChanged)
                .width(90),
            button("Discover")
                .on_press_maybe((!busy).then_some(Message::DiscoverPressed))
                .padding([6, 14]),
            text(self.status_text()).width(Length::Fill),
        ]
        .spacing(8);

        let controls = row![
            text("Flash").width(45),
            pick_list(Flash::ALL, Some(self.flash), Message::FlashChanged).width(130),
            text("Buzzer").width(55),
            pick_list(
                BuzzerPattern::ALL,
                Some(self.buzzer),
                Message::BuzzerChanged
            )
            .width(160),
        ]
        .spacing(8);

        let content = column![
            text("Patlite Controller").size(28),
            endpoint,
            controls,
            row![self.tiers_panel(), self.presets_panel(can_send),]
                .spacing(16)
                .height(260),
            self.log_panel(),
            row![
                button("Off")
                    .on_press_maybe(can_send.then_some(Message::OffPressed))
                    .width(120)
                    .padding([8, 18]),
                button("Send")
                    .on_press_maybe(can_send.then_some(Message::SendPressed))
                    .width(140)
                    .padding([8, 18]),
            ]
            .spacing(10)
        ]
        .padding(18)
        .spacing(12);

        container(content)
            .width(Length::Fill)
            .height(Length::Fill)
            .into()
    }

    fn tiers_panel(&self) -> Element<'_, Message> {
        let mut tiers = column![text("Tiers").size(20)].spacing(8);

        for index in 0..5 {
            let selected = self.tiers[index];
            let applied = self.applied.tiers[index];
            let tier_row = row![
                text(format!("Tier {}", index + 1)).width(60),
                pick_list(La6Colour::ALL, Some(selected), move |value| {
                    Message::TierChanged(index, value)
                })
                .width(150),
                color_chip(applied),
                text(applied.name()).width(90),
            ]
            .spacing(8);
            tiers = tiers.push(tier_row);
        }

        container(tiers)
            .padding(12)
            .width(Length::FillPortion(2))
            .style(container::bordered_box)
            .into()
    }

    fn presets_panel(&self, can_send: bool) -> Element<'_, Message> {
        let mut presets = column![text("Presets").size(20)].spacing(8);

        for (index, preset) in PRESETS.iter().enumerate() {
            presets = presets.push(
                button(preset.name)
                    .on_press_maybe(can_send.then_some(Message::PresetPressed(index)))
                    .width(Length::Fill)
                    .padding([6, 12]),
            );
        }

        presets = presets
            .push(Space::new().height(4))
            .push(text("Current Flash").size(14))
            .push(text(self.applied.flash.name()))
            .push(text("Current Buzzer").size(14))
            .push(text(self.applied.buzzer.name()))
            .push(Space::new().height(8))
            .push(text("Discovered Devices").size(16));

        if self.discovered_devices.is_empty() {
            presets = presets.push(text("No devices discovered yet.").size(13));
        } else {
            for (index, device) in self.discovered_devices.iter().enumerate() {
                let label = format!("{} - {}", device.label(), device.source);
                presets = presets.push(
                    button(text(label).size(13))
                        .on_press(Message::DeviceSelected(index))
                        .width(Length::Fill)
                        .padding([5, 8]),
                );
            }
        }

        container(scrollable(presets))
            .padding(12)
            .width(Length::FillPortion(1))
            .style(container::bordered_box)
            .into()
    }

    fn log_panel(&self) -> Element<'_, Message> {
        let mut lines = column![].spacing(2);
        for line in &self.logs {
            lines = lines.push(text(line.as_str()).size(13));
        }

        container(scrollable(lines).height(Length::Fill))
            .padding(12)
            .height(Length::Fill)
            .width(Length::Fill)
            .style(container::bordered_box)
            .into()
    }

    fn start_send(&mut self, pattern: PatlitePattern, label: &str) -> Task<Message> {
        if !self.can_send() {
            if let Err(error) = validate_endpoint(&self.ip, &self.port) {
                self.log(error.to_string());
            }
            return Task::none();
        }

        let (host, port) = match validate_endpoint(&self.ip, &self.port) {
            Ok(endpoint) => endpoint,
            Err(error) => {
                self.log(error.to_string());
                return Task::none();
            }
        };

        self.is_sending = true;
        self.log(format!("Sending {label} to {host}:{port}..."));

        Task::perform(
            async move {
                send_pattern(&host, port, pattern)
                    .map(|_| pattern)
                    .map_err(|error| error.to_string())
            },
            Message::SendFinished,
        )
    }

    fn start_discovery(&mut self) -> Task<Message> {
        if self.is_busy() {
            return Task::none();
        }

        self.is_discovering = true;
        self.log("Discovery started.");

        let current_port = parse_port(&self.port).unwrap_or(DEFAULT_COMMAND_PORT);
        let config = DiscoveryConfig {
            current_host: self.ip.clone(),
            current_port,
            scan_local_subnet: true,
        };

        Task::perform(
            async move { discover_devices(config) },
            Message::DiscoveryFinished,
        )
    }

    fn current_pattern(&self) -> PatlitePattern {
        PatlitePattern::new(self.tiers, self.flash, self.buzzer)
    }

    fn can_send(&self) -> bool {
        !self.is_busy() && validate_endpoint(&self.ip, &self.port).is_ok()
    }

    fn is_busy(&self) -> bool {
        self.is_sending || self.is_discovering
    }

    fn status_text(&self) -> String {
        if self.is_sending {
            "Sending...".to_string()
        } else if self.is_discovering {
            "Discovering...".to_string()
        } else if validate_endpoint(&self.ip, &self.port).is_ok() {
            "Ready".to_string()
        } else {
            "Endpoint invalid".to_string()
        }
    }

    fn log(&mut self, message: impl Into<String>) {
        self.logs.push(message.into());
        if self.logs.len() > MAX_LOG_LINES {
            self.logs.drain(0..self.logs.len() - MAX_LOG_LINES);
        }
    }
}

fn color_chip<'a>(colour: La6Colour) -> Element<'a, Message> {
    let (red, green, blue) = colour.rgb();
    let fill = Color::from_rgb(red, green, blue);

    container(Space::new().width(18).height(18))
        .width(18)
        .height(18)
        .style(move |_theme: &Theme| {
            container::Style::default()
                .background(Background::Color(fill))
                .border(Border {
                    color: Color::from_rgb(0.08, 0.10, 0.12),
                    width: 1.0,
                    radius: 9.0.into(),
                })
        })
        .into()
}
