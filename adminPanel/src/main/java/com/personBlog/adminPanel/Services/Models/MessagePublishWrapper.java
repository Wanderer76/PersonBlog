package com.personBlog.adminPanel.Services.Models;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

@AllArgsConstructor
@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
public class MessagePublishWrapper<T> {
    @JsonProperty("EventData")
    private  T eventData;
    @JsonProperty("EventType")
    private  String eventType;

}
