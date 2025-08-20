package com.personBlog.adminPanel.Controllers;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("api/health")
public class HealthController {

    @GetMapping("/check")
    public ResponseEntity<HealthState> checkHealth() {
        return new ResponseEntity<HealthState>(new HealthState("live"), HttpStatus.OK);
    }
    record HealthState(String status) {}

}

