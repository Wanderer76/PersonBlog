package com.personBlog.adminPanel.Services.Models;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.time.OffsetDateTime;
import java.util.UUID;

@Getter
@Setter
@AllArgsConstructor
public class PostBanRequestEvent implements Serializable {
    @JsonProperty("PostId")
    private UUID postId;
    @JsonProperty("ObjectName")
    private String objectName;
    @JsonProperty("ReasonId")
    private UUID reasonId;
    @JsonProperty("UserMessage")
    private String userMessage;
    @JsonProperty("CreatedAt")
    private OffsetDateTime createdAt;
    @JsonProperty("CreatorUserId")
    private UUID creatorUserId;
}
